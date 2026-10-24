using UnityEngine;
using UnityEngine.InputSystem;

public class CameraTargetController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _player;

    [Header("Camera")]
    [SerializeField] private Vector3 _offset = new(0f, 1.6f, 0f);

    [Header("Sensitivity")]
    [SerializeField] private float _mouseSensitivity = 0.1f;
    [SerializeField] private float _gamepadSensitivity = 180f;

    [Header("Vertical Limits")]
    [SerializeField] private float _minPitch = -60f;
    [SerializeField] private float _maxPitch = 70f;
    [SerializeField] private float _maxAimPitch = 0.8f;
    [SerializeField] private float _minAimPitch = 0.9f;

    private InputSystem_Actions _input;

    private float _yaw;
    private float _pitch;

    /// <summary>
    /// Converts camera pitch into a normalized value for vertical aiming animations.
    /// Uses separate multipliers for looking up and down.
    /// </summary>
    public float AimPitch => _pitch < 0f
        ? Mathf.Clamp01(-_pitch / -_minPitch) * _maxAimPitch
        : -Mathf.Clamp01(_pitch / _maxPitch) * _minAimPitch;

    private void Awake()
    {
        _input = new InputSystem_Actions();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _yaw = transform.eulerAngles.y;
        _pitch = transform.eulerAngles.x;

        // Convert Unity's 0-360 rotation to a signed pitch angle.
        if (_pitch > 180f)
            _pitch -= 360f;
    }

    private void OnEnable()
    {
        _input.Player.Enable();
    }

    private void OnDisable()
    {
        _input.Player.Disable();
    }

    private void OnDestroy()
    {
        _input.Dispose();
    }

    private void Update()
    {
        HandleRotation();
    }

    private void LateUpdate()
    {
        transform.position = _player.position + _offset;
    }

    /// <summary>
    /// Rotates the camera target using mouse or gamepad input.
    /// Applies frame-independent sensitivity for gamepad input and clamps vertical rotation.
    /// </summary>
    private void HandleRotation()
    {
        Vector2 lookInput = _input.Player.Look.ReadValue<Vector2>();

        bool isMouse = _input.Player.Look.activeControl?.device is Mouse;

        float sensitivity = isMouse
            ? _mouseSensitivity
            : _gamepadSensitivity * Time.deltaTime;

        _yaw += lookInput.x * sensitivity;
        _pitch -= lookInput.y * sensitivity;

        _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
}