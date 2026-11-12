using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Which action maps are on. GameManager sets the mode on every state change and on pause.</summary>
public enum InputMode
{
    Gameplay,   // Player map on, UI map off.
    UI,         // UI map on, Player map off except Pause.
    Disabled    // Everything off except Pause. Used during scene loads.
}

/// <summary>
/// Owns the only InputSystem_Actions instance. Every other script reads input from here, through its
/// held values (Move, Look, IsFiring, and the rest) or its press events (OnJumpPressed, OnFirePressed, and the rest).
/// Implements the generated IPlayerActions interface, so the Input System calls the On... methods below.
/// </summary>
// INTEGRATION: new script. Replaces PlayerInput, CameraTargetController, and PartSwapTest each creating their own
// InputSystem_Actions, and scripts reading Keyboard.current. Lives in the Managers prefab.
public class InputManager : MGR<InputManager>, InputSystem_Actions.IPlayerActions
{
    private InputSystem_Actions _actions; // The generated wrapper around the project's Input Actions asset.

    // Latest raw values from the callbacks. The public properties below hide them outside Gameplay.
    private Vector2 _move;
    private Vector2 _look;
    private bool _isSprinting;
    private bool _isAiming;
    private bool _isFiring;

    /// <summary>The current input mode. Change it with SetMode.</summary>
    public InputMode Mode { get; private set; } = InputMode.Gameplay;

    // Held values. Each reads zero or false outside Gameplay, so the player and camera stop
    // without each script checking the mode.
    public Vector2 Move => IsGameplay ? _move : Vector2.zero;
    public Vector2 Look => IsGameplay ? _look : Vector2.zero;
    public bool IsSprinting => IsGameplay && _isSprinting;
    public bool IsAiming => IsGameplay && _isAiming;
    public bool IsFiring => IsGameplay && _isFiring;

    /// <summary>
    /// True when the last look input came from the mouse. CameraTargetController uses it to pick mouse or gamepad sensitivity.
    /// </summary>
    public bool LookIsMouse { get; private set; } = true;

    /// <summary>The Input Actions asset behind this instance. Rebinding UI such as ActionLabelDisplay reads it.</summary>
    public InputActionAsset Asset => _actions.asset;

    // Press events. Each fires once, when its action is performed.
    // PlayerInput forwards the jump, dash, and fire events. Shoot listens to OnFirePressed, InteractionDetector to
    // OnInteractPressed, GameManager to OnPausePressed, and Inventory to OnConsumablePressed.
    public event Action OnJumpPressed;
    public event Action OnJumpReleased;
    public event Action OnDashPressed;
    public event Action OnFirePressed;
    public event Action OnInteractPressed;
    public event Action OnPausePressed;
    /// <summary>Fires when a consumable key is pressed. The value is the slot: 0, 1, or 2.</summary>
    public event Action<int> OnConsumablePressed;
    /// <summary>Fires after SetMode. UIManager can use it to show or hide the cursor.</summary>
    public event Action<InputMode> OnModeChanged;

    private bool IsGameplay => Mode == InputMode.Gameplay;

    /// <summary>Creates the actions, registers this object for the Player callbacks, and starts in Gameplay.</summary>
    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }

        _actions = new InputSystem_Actions();
        _actions.Player.AddCallbacks(this);

        // Starts in Gameplay so pressing Play in a gameplay scene works. GameManager sets the real mode on each state change.
        SetMode(InputMode.Gameplay);
    }

    /// <summary>
    /// Turns action maps on and off for the given mode. Pause stays on in every mode, so the pause key always works.
    /// </summary>
    /// <param name="mode">The mode to switch to.</param>
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
    // The Input System calls these for every phase of each action (started, performed, canceled).
    // Held values store the latest value. Press events fire only on performed, so an action with a Hold
    // interaction needs the key held for the hold time before its event fires.

    /// <summary>Stores the move input.</summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        _move = context.ReadValue<Vector2>();
    }

    /// <summary>Stores the look input and which device it came from.</summary>
    public void OnLook(InputAction.CallbackContext context)
    {
        _look = context.ReadValue<Vector2>();
        if (context.control != null)
        {
            LookIsMouse = context.control.device is Mouse;
        }
    }

    /// <summary>Stores whether sprint is held.</summary>
    public void OnSprint(InputAction.CallbackContext context)
    {
        _isSprinting = context.ReadValueAsButton();
    }

    /// <summary>Stores whether aim is held.</summary>
    public void OnAim(InputAction.CallbackContext context)
    {
        _isAiming = context.ReadValueAsButton();
    }

    /// <summary>Stores whether fire is held, and raises OnFirePressed on the press.</summary>
    public void OnFire(InputAction.CallbackContext context)
    {
        _isFiring = context.ReadValueAsButton();
        if (context.performed)
        {
            OnFirePressed?.Invoke();
        }
    }

    /// <summary>Raises OnJumpPressed on the press and OnJumpReleased on the release, for variable jump height.</summary>
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnJumpPressed?.Invoke();
        }
        else if (context.canceled)
        {
            OnJumpReleased?.Invoke();
        }
    }

    /// <summary>Raises OnDashPressed.</summary>
    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnDashPressed?.Invoke();
        }
    }

    /// <summary>Raises OnInteractPressed. InteractionDetector uses the closest interactable.</summary>
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnInteractPressed?.Invoke();
        }
    }

    /// <summary>Raises OnPausePressed. GameManager toggles pause.</summary>
    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnPausePressed?.Invoke();
        }
    }

    /// <summary>Raises OnConsumablePressed for slot 0.</summary>
    public void OnUseConsumable1(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnConsumablePressed?.Invoke(0);
        }
    }

    /// <summary>Raises OnConsumablePressed for slot 1.</summary>
    public void OnUseConsumable2(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnConsumablePressed?.Invoke(1);
        }
    }

    /// <summary>Raises OnConsumablePressed for slot 2.</summary>
    public void OnUseConsumable3(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnConsumablePressed?.Invoke(2);
        }
    }

    #endregion

    /// <summary>Unregisters the callbacks and releases the actions.</summary>
    private void OnDestroy()
    {
        if (_actions == null)
        {
            return; // A duplicate destroyed in Awake never created actions.
        }

        _actions.Player.RemoveCallbacks(this);
        _actions.Disable();
        _actions.Dispose();
    }
}
