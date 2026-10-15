using UnityEngine;

public class FollowCameraRotation : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private float _baseRotationSpeed = 15f;
    [SerializeField] private float _aimingRotationSpeed = 50f;
    [SerializeField] private float _rollingRotationSpeed = 10000f;
    
    [Header("References")]
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private PlayerCameraController _cameraController;
    [SerializeField] private Dash _dash;

    private void Awake()
    {
        if (!_playerInput)
            _playerInput = FindFirstObjectByType<PlayerInput>();
        if (!_cameraController)
            _cameraController = FindFirstObjectByType<PlayerCameraController>();
        if (!_dash)
            _dash = FindFirstObjectByType<Dash>();
    }
    
    void LateUpdate()
    {
        if (_dash.IsDashing)
        {
            SetRollingRotation();
        }
        else if (_cameraController.IsAiming)
        {
            FollowRotation(_aimingRotationSpeed);
        }
        else if (_playerInput.Move.sqrMagnitude > 0.01f)
        {
            FollowRotation(_baseRotationSpeed);
        }
    }

    private void FollowRotation(float rotationSpeed)
    {
        float targetY = _target.eulerAngles.y;
        
        Quaternion targetRotation = Quaternion.Euler(0, targetY, 0);
        
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void SetRollingRotation()
    {
        Quaternion targetRotation = Quaternion.LookRotation(_dash.DashDirection);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rollingRotationSpeed * Time.deltaTime);
    }
}
