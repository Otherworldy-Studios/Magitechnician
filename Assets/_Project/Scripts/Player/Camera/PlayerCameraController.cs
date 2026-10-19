using System;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _mainCamera;
    [SerializeField] private CinemachineCamera _aimCamera;
    [SerializeField] private PlayerInput _input;
    [SerializeField] private Dash _playerDash;

    public bool IsAiming { get; private set; }
    public event Action<bool> OnAimChanged;

    private void Awake()
    {
        IsAiming = false;

        _mainCamera.Priority = 10;
        _aimCamera.Priority = 0;
        
        if(!_playerDash)
            _playerDash = FindFirstObjectByType<Dash>();
    }

    private void Update()
    {
        if (_playerDash.IsDashing)
        {
            SetAiming(false);
            return;
        }
        
        SetAiming(_input.IsAiming);
    }

    private void SetAiming(bool aiming)
    {
        if (IsAiming == aiming)
            return;
        
        IsAiming = aiming;

        _mainCamera.Priority = aiming ? 0 : 10;
        _aimCamera.Priority = aiming ? 10 : 0;
        OnAimChanged?.Invoke(aiming);
    }
}