using UnityEngine;

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

    private float _yaw;
    private float _pitch;

    public float AimPitch => _pitch < 0f
        ? Mathf.Clamp01(-_pitch / -_minPitch) * _maxAimPitch
        : -Mathf.Clamp01(_pitch / _maxPitch) * _minAimPitch;

    private void Awake()
    {
        _yaw = transform.eulerAngles.y;
        _pitch = transform.eulerAngles.x;

        // Convert Unity's 0-360 rotation to a signed pitch angle.
        if (_pitch > 180f)
            _pitch -= 360f;
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
    /// Sets the camera's direction directly. PlayerSpawner.Teleport calls this,
    /// so the camera faces the same way as the player after a teleport.
    /// </summary>
    public void SetYawPitch(float yaw, float pitch)
    {
        _yaw = yaw;
        _pitch = Mathf.Clamp(pitch, _minPitch, _maxPitch);
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    private void HandleRotation()
    {
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