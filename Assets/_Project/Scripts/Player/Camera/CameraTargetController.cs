using UnityEngine;

/// <summary>
/// The point the Cinemachine camera follows and looks through. Follows the player at an offset, and turns with Look input.
/// </summary>
// INTEGRATION: no longer creates its own InputSystem_Actions or locks the cursor. Look input comes from InputManager,
// and UIManager locks and shows the cursor per screen.
public class CameraTargetController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _player; // The transform followed. The part of the player that moves.

    [Header("Camera")]
    [SerializeField] private Vector3 _offset = new(0f, 1.6f, 0f); // Offset from the player, about eye height.

    [Header("Sensitivity")]
    [SerializeField] private float _mouseSensitivity = 0.1f;  // Degrees per unit of mouse movement.
    [SerializeField] private float _gamepadSensitivity = 180f; // Degrees per second at full stick.

    [Header("Vertical Limits")]
    [SerializeField] private float _minPitch = -60f;
    [SerializeField] private float _maxPitch = 70f;
    // Scale AimPitch when looking down (_maxAimPitch) and up (_minAimPitch).
    [SerializeField] private float _maxAimPitch = 0.8f;
    [SerializeField] private float _minAimPitch = 0.9f;

    private float _yaw;   // Left-right angle, in degrees.
    private float _pitch; // Up-down angle, in degrees. Negative looks up.

    /// <summary>
    /// Converts camera pitch into a value for the vertical aiming animation. Uses separate multipliers for looking up
    /// and down. PlayerAnimator sends it to the Animator as AimPitch.
    /// </summary>
    public float AimPitch => _pitch < 0f
        ? Mathf.Clamp01(-_pitch / -_minPitch) * _maxAimPitch
        : -Mathf.Clamp01(_pitch / _maxPitch) * _minAimPitch;

    /// <summary>Starts from the object's current rotation.</summary>
    private void Awake()
    {
        _yaw = transform.eulerAngles.y;
        _pitch = transform.eulerAngles.x;

        // INTEGRATION: removed creating InputSystem_Actions and locking the cursor here.
        // Convert Unity's 0-360 rotation to a signed pitch angle.
        if (_pitch > 180f)
            _pitch -= 360f;
    }

    /// <summary>Turns with Look input.</summary>
    private void Update()
    {
        HandleRotation();
    }

    /// <summary>Follows the player, after the player has moved this frame.</summary>
    private void LateUpdate()
    {
        transform.position = _player.position + _offset;
    }

    /// <summary>
    /// Sets the camera's direction directly. PlayerSpawner.Teleport calls this,
    /// so the camera faces the same way as the player after a teleport.
    /// </summary>
    // INTEGRATION: new, for PlayerSpawner.
    public void SetYawPitch(float yaw, float pitch)
    {
        _yaw = yaw;
        _pitch = Mathf.Clamp(pitch, _minPitch, _maxPitch);
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    /// <summary>
    /// Rotates the camera target with mouse or gamepad input. Gamepad input is scaled by frame time, mouse input isn't,
    /// because the mouse already reports movement per frame. Clamps the pitch.
    /// </summary>
    private void HandleRotation()
    {
        // INTEGRATION: was _input.Player.Look and a Mouse device check. InputManager.LookIsMouse (new) does the check.
        InputManager input = InputManager.Instance;
        Vector2 lookInput = input.Look;

        float sensitivity = input.LookIsMouse
            ? _mouseSensitivity
            : _gamepadSensitivity * Time.deltaTime;

        _yaw += lookInput.x * sensitivity;
        _pitch -= lookInput.y * sensitivity;
        _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
}