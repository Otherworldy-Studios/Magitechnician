using UnityEngine;

//Moves the object in a direction at the speed defined by the Stats component.
//Uses a CharacterController to handle collisions and slopes.
[RequireComponent(typeof(Stats))]
[RequireComponent(typeof(CharacterController))]
public class Movement : MonoBehaviour
{
    [Header("Direction")]
    //If true, directions are treated as input relative to the camera (x = right, z = forward).
    //Turn this on for the player. AI usually leaves it off and passes world-space directions.
    [SerializeField] private bool _cameraRelative = false;
    //Camera used for camera-relative movement. Falls back to Camera.main if not set.
    [SerializeField] private Transform _cameraTransform;

    [Header("Rotation")]
    [SerializeField] private bool _faceMovementDirection = true;
    //Degrees per second.
    [SerializeField] private float _turnSpeed = 720f;

    [Header("Gravity")]
    [SerializeField] private float _gravity = -20f;
    //Small downward push while grounded keeps the controller snapped to slopes and steps.
    [SerializeField] private float _groundedVerticalVelocity = -2f;

    private Stats _stats;
    private Health _health; //Optional. Entity can't move when dead.
    private CharacterController _controller;

    private Vector3 _movementDirection;
    private float _verticalVelocity;

    private void Awake()
    {
        _stats = GetComponent<Stats>();
        _health = GetComponent<Health>();
        _controller = GetComponent<CharacterController>();

        if (_cameraRelative && _cameraTransform == null && Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
    }

    //Called by player input or AI to set the movement direction.
    //Clamped to a length of 1: diagonals aren't faster.
    //The Y component is ignored; vertical movement comes from gravity.
    public void SetMovementDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (_cameraRelative && _cameraTransform != null)
        {
            direction = ToCameraRelative(direction);
        }

        _movementDirection = Vector3.ClampMagnitude(direction, 1f);
    }

    private void Update()
    {
        //Dead entities can't move. Clearing the direction means a revived entity won't move until the direction is set again.
        //Gravity still applies, so a body that dies mid-air falls to the ground.
        bool isDead = _health != null && _health.isDead;
        if (isDead)
        {
            _movementDirection = Vector3.zero;
        }

        ApplyGravity();

        //Since speed is read every frame, modifiers take effect immediately.
        Vector3 velocity = _movementDirection * _stats.speed;
        velocity.y = _verticalVelocity;
        _controller.Move(velocity * Time.deltaTime);

        if (!isDead)
        {
            FaceMovementDirection();
        }
    }

    private void ApplyGravity()
    {
        if (_controller.isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = _groundedVerticalVelocity;
        }
        else
        {
            _verticalVelocity += _gravity * Time.deltaTime;
        }
    }

    private void FaceMovementDirection()
    {
        if (!_faceMovementDirection || _movementDirection.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(_movementDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _turnSpeed * Time.deltaTime);
    }

    //Converts input (x = right, z = forward) into a world direction on the ground plane, based on where the camera faces.
    private Vector3 ToCameraRelative(Vector3 input)
    {
        Vector3 forward = _cameraTransform.forward;
        forward.y = 0f;

        //If the camera looks straight down, its forward flattens to zero; use its up vector instead.
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = _cameraTransform.up;
            forward.y = 0f;
        }
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward);
        return forward * input.z + right * input.x;
    }

}
