using System;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Switches between the normal and aiming Cinemachine cameras while Aim is held, and reports aim changes.
/// </summary>
public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _mainCamera;
    [SerializeField] private CinemachineCamera _aimCamera;
    [SerializeField] private PlayerInput _input;

    /// <summary>True while the aiming camera is active.</summary>
    public bool IsAiming { get; private set; }

    /// <summary>Fires with the new value when IsAiming changes. PlayerAnimator listens.</summary>
    public event Action<bool> OnAimChanged;

    /// <summary>Starts on the normal camera.</summary>
    private void Awake()
    {
        IsAiming = false;

        _mainCamera.Priority = 10;
        _aimCamera.Priority = 0;
    }

    /// <summary>Follows the Aim button each frame.</summary>
    private void Update()
    {
        SetAiming(_input.IsAiming);
    }

    /// <summary>
    /// Switches between normal and aiming cameras using Cinemachine priorities.
    /// Notifies listeners only when the aiming state changes.
    /// </summary>
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