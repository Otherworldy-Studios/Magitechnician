using System;
using UnityEngine;

/// <summary>
/// The player's view of input. Forwards values and events from InputManager,
/// so player scripts keep their PlayerInput reference and never touch the actions directly.
/// </summary>
// INTEGRATION: no longer creates its own InputSystem_Actions. InputManager (new) owns the only copy, so the UI and the
// player read the same actions, and turning player input off for menus covers every script. PlayerMovement still
// subscribes here, so it didn't need to change how it gets input.
public class PlayerInput : MonoBehaviour
{
    // INTEGRATION: these now read InputManager. Move and Look were Vector3 and are now Vector2, matching the actions.
    /// <summary>Movement stick or WASD.</summary>
    public Vector2 Move => InputManager.Instance.Move;
    /// <summary>Mouse or right stick change this frame.</summary>
    public Vector2 Look => InputManager.Instance.Look;
    /// <summary>True while Sprint is held.</summary>
    public bool IsSprinting => InputManager.Instance.IsSprinting;
    /// <summary>True while Aim is held.</summary>
    public bool IsAiming => InputManager.Instance.IsAiming;
    /// <summary>True while Fire is held.</summary>
    public bool IsFiring => InputManager.Instance.IsFiring;

    // PlayerMovement subscribes to the jump and dash events.
    /// <summary>Fires when Jump is pressed.</summary>
    public event Action OnJumpPressed;
    /// <summary>Fires when Jump is released. PlayerMovement uses it to cut a jump short.</summary>
    public event Action OnJumpReleased;
    /// <summary>Fires when Dash is pressed.</summary>
    public event Action OnDashPressed;
    /// <summary>Fires when Fire is pressed.</summary>
    public event Action OnFirePressed;

    /// <summary>Starts forwarding InputManager's events.</summary>
    private void OnEnable()
    {
        // INTEGRATION: was _input.Player.Jump.performed and so on.
        InputManager input = InputManager.Instance;
        input.OnJumpPressed += HandleJumpPressed;
        input.OnJumpReleased += HandleJumpReleased;
        input.OnDashPressed += HandleDashPressed;
        input.OnFirePressed += HandleFirePressed;
    }

    /// <summary>Stops forwarding. Skipped if InputManager was already destroyed, such as when the game closes.</summary>
    private void OnDisable()
    {
        if (!InputManager.HasInstance) // INTEGRATION: new. InputManager can be destroyed first when the game closes.
        {
            return;
        }

        InputManager input = InputManager.Instance;
        input.OnJumpPressed -= HandleJumpPressed;
        input.OnJumpReleased -= HandleJumpReleased;
        input.OnDashPressed -= HandleDashPressed;
        input.OnFirePressed -= HandleFirePressed;
    }

    /// <summary>Passes InputManager.OnJumpPressed on to OnJumpPressed.</summary>
    private void HandleJumpPressed() => OnJumpPressed?.Invoke();

    /// <summary>Passes InputManager.OnJumpReleased on to OnJumpReleased.</summary>
    private void HandleJumpReleased() => OnJumpReleased?.Invoke();

    /// <summary>Passes InputManager.OnDashPressed on to OnDashPressed.</summary>
    private void HandleDashPressed() => OnDashPressed?.Invoke();

    /// <summary>Passes InputManager.OnFirePressed on to OnFirePressed.</summary>
    private void HandleFirePressed() => OnFirePressed?.Invoke();
}
