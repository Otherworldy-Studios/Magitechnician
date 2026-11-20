using UnityEngine;

/// <summary>
/// Turns the player's body toward the camera's facing: fastest while dashing, fast while aiming or hip-firing, and
/// slower while moving. Stays put when standing still.
/// </summary>
public class FollowCameraRotation : MonoBehaviour
{
    [SerializeField] private Transform _target; // The camera target. Its yaw is the facing to turn toward.
    [SerializeField] private float _baseRotationSpeed = 15f;
    [SerializeField] private float _aimingRotationSpeed = 50f;
    [SerializeField] private float _rollingRotationSpeed = 10000f;

    // INTEGRATION: was a serialized PlayerInput reference. Now InputManager.Instance, set in Awake.
    private InputManager _input;

    [Header("References")]
    [SerializeField] private PlayerCameraController _cameraController;
    [SerializeField] private Dash _dash;
    [SerializeField] private PlayerCombatState _combatState;

    /// <summary>Finds any references left empty.</summary>
    private void Awake()
    {
        if (!_input)
            _input = InputManager.Instance;
        if (!_cameraController)
            _cameraController = FindFirstObjectByType<PlayerCameraController>();
        if (!_dash)
            _dash = FindFirstObjectByType<Dash>();
        if (!_combatState)
            _combatState = FindFirstObjectByType<PlayerCombatState>();
    }

    /// <summary>
    /// Rotates the player based on the current action.
    /// Dash direction takes priority over aiming and normal movement.
    /// </summary>
    private void LateUpdate()
    {
        if (_dash.IsDashing)
        {
            FollowRotation(_rollingRotationSpeed);
        }
        else if (_cameraController.IsAiming ||
                 _combatState.CurrentCombatState == PlayerCombatStates.HipFire)
        {
            FollowRotation(_aimingRotationSpeed);
        }
        else if (_input.Move.sqrMagnitude > 0.01f)
        {
            FollowRotation(_baseRotationSpeed);
        }
    }

    /// <summary>
    /// Smoothly rotates the player toward the target's horizontal facing direction.
    /// </summary>
    private void FollowRotation(float rotationSpeed)
    {
        float targetY = _target.eulerAngles.y;

        Quaternion targetRotation = Quaternion.Euler(0, targetY, 0);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}