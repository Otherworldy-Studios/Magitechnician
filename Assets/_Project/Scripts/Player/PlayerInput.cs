using System;
using UnityEngine;

/// <summary>
/// The player's view of input. Forwards values and events from InputManager,
/// so player scripts keep their PlayerInput reference and never touch the actions directly.
/// </summary>
public class PlayerInput : MonoBehaviour
{
    public Vector2 Move => InputManager.Instance.Move;
    public Vector2 Look => InputManager.Instance.Look;
    public bool IsSprinting => InputManager.Instance.IsSprinting;
    public bool IsAiming => InputManager.Instance.IsAiming;
    public bool IsFiring => InputManager.Instance.IsFiring;

    public event Action JumpPressed;
    public event Action JumpReleased;
    public event Action DashPressed;
    public event Action FirePressed;

    private void OnEnable()
    {
        InputManager input = InputManager.Instance;
        input.JumpPressed += OnJumpPressed;
        input.JumpReleased += OnJumpReleased;
        input.DashPressed += OnDashPressed;
        input.FirePressed += OnFirePressed;
    }

    private void OnDisable()
    {
        if (!InputManager.HasInstance)
        {
            return;   
        }

        InputManager input = InputManager.Instance;
        input.JumpPressed -= OnJumpPressed;
        input.JumpReleased -= OnJumpReleased;
        input.DashPressed -= OnDashPressed;
        input.FirePressed -= OnFirePressed;
    }

    private void OnJumpPressed() => JumpPressed?.Invoke();
    private void OnJumpReleased() => JumpReleased?.Invoke();
    private void OnDashPressed() => DashPressed?.Invoke();
    private void OnFirePressed() => FirePressed?.Invoke();
}