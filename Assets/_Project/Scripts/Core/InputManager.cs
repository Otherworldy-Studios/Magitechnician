using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum InputMode
{
    Gameplay,   // Player map on, UI map off.
    UI,         // UI map on, Player map off except Pause.
    Disabled    // Everything off except Pause. Used during scene loads.
}

/// <summary>
/// Owns the only InputSystem_Actions instance. Every other script reads input from here.
/// </summary>
public class InputManager : MGR<InputManager>, InputSystem_Actions.IPlayerActions
{
    private InputSystem_Actions _actions;

    private Vector2 _move;
    private Vector2 _look;
    private bool _isSprinting;
    private bool _isAiming;
    private bool _isFiring;

    public InputMode Mode { get; private set; } = InputMode.Gameplay;

    // Held values. Each reads zero or false outside Gameplay, so the player and camera stop.
    public Vector2 Move => IsGameplay ? _move : Vector2.zero;
    public Vector2 Look => IsGameplay ? _look : Vector2.zero;
    public bool IsSprinting => IsGameplay && _isSprinting;
    public bool IsAiming => IsGameplay && _isAiming;
    public bool IsFiring => IsGameplay && _isFiring;

    // CameraTargetController uses this to pick mouse or gamepad sensitivity.
    public bool LookIsMouse { get; private set; } = true;

   
    public InputActionAsset Asset => _actions.asset;

    // Presses.
    public event Action JumpPressed;
    public event Action JumpReleased;
    public event Action DashPressed;
    public event Action FirePressed;
    public event Action InteractPressed;
    public event Action PausePressed;
    public event Action<int> ConsumablePressed;   // Slot 0, 1, or 2.
    public event Action<InputMode> OnModeChanged;

    private bool IsGameplay => Mode == InputMode.Gameplay;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }

        _actions = new InputSystem_Actions();
        _actions.Player.AddCallbacks(this);

        // Starts in Gameplay so the current game keeps working.
        // GameManager sets the real mode on each state change.
        SetMode(InputMode.Gameplay);
    }

    /// <summary>
    /// Turns action maps on and off for the given mode. Pause stays on in every mode.
    /// </summary>
    public void SetMode(InputMode mode)
    {
        Mode = mode;

        _actions.Player.Disable();
        _actions.UI.Disable();

        switch (mode)
        {
            case InputMode.Gameplay:
                _actions.Player.Enable();
                break;
            case InputMode.UI:
                _actions.UI.Enable();
                _actions.Player.Pause.Enable();
                break;
            case InputMode.Disabled:
                _actions.Player.Pause.Enable();
                break;
        }

        OnModeChanged?.Invoke(mode);
    }

    #region IPlayerActions

  
    public void OnMove(InputAction.CallbackContext context)
    {
        _move = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        _look = context.ReadValue<Vector2>();
        if (context.control != null)
        {
            LookIsMouse = context.control.device is Mouse;
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        _isSprinting = context.ReadValueAsButton();
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        _isAiming = context.ReadValueAsButton();
    }

  
    public void OnFire(InputAction.CallbackContext context)
    {
        _isFiring = context.ReadValueAsButton();
        if (context.performed)
        {
            FirePressed?.Invoke();
        }
    }

  
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            JumpPressed?.Invoke();
        }
        else if (context.canceled)
        {
            JumpReleased?.Invoke();
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            DashPressed?.Invoke();
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            InteractPressed?.Invoke();
        }
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PausePressed?.Invoke();
        }
    }

    public void OnUseConsumable1(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ConsumablePressed?.Invoke(0);
        }
    }

    public void OnUseConsumable2(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ConsumablePressed?.Invoke(1);
        }
    }

    public void OnUseConsumable3(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            ConsumablePressed?.Invoke(2);
        }
    }

    #endregion

    private void OnDestroy()
    {
        if (_actions == null)
        {
            return;  
        }

        _actions.Player.RemoveCallbacks(this);
        _actions.Disable();
        _actions.Dispose();
    }
}