using UnityEngine;

/// <summary>Whether the player is relaxed, aiming down sights, or firing from the hip.</summary>
public enum PlayerCombatStates
{
    Relaxed,
    Aiming,
    HipFire
}

/// <summary>
/// Tracks the player's combat state for the animator: Aiming while Aim is held, HipFire for a short time after a shot,
/// and Relaxed otherwise. A dash without aiming drops straight back to Relaxed.
/// </summary>
public class PlayerCombatState : MonoBehaviour
{
    [Header("Settings")] 
    [Tooltip("Seconds the player stays in HipFire after a shot.")]
    [SerializeField] private float _hipFireDuration = 1.5f;
    
    [Header("References")]
    [SerializeField] private Shoot _shoot;
    [SerializeField] private PlayerCameraController _playerCameraController;
    [SerializeField] private Dash _dash;

    /// <summary>The current combat state. PlayerAnimator sends it to the Animator as CombatState.</summary>
    [field: SerializeField] public PlayerCombatStates CurrentCombatState { get; private set; }

    private float _lastShotTime = float.NegativeInfinity; // Time.time of the last shot. Starts far in the past.

    /// <summary>Listens for shots.</summary>
    // INTEGRATION: Shoot now fires it on every shot
    // the Gun makes, so automatic fire keeps the player in HipFire.
    private void OnEnable()
    {
        _shoot.OnFired += HandleFire;
    }

    /// <summary>Stops listening.</summary>
    private void OnDisable()
    {
        _shoot.OnFired -= HandleFire;
    }

    /// <summary>Picks the combat state for this frame.</summary>
    private void Update()
    {
        if (_dash.IsDashing && !_playerCameraController.IsAiming)
        {
            _lastShotTime = float.NegativeInfinity;
            CurrentCombatState = PlayerCombatStates.Relaxed;
            return;
        }

        if (_playerCameraController.IsAiming)
        {
            CurrentCombatState = PlayerCombatStates.Aiming;
        }
        else if (Time.time - _lastShotTime < _hipFireDuration)
        {
            CurrentCombatState = PlayerCombatStates.HipFire;
        }
        else
        {
            CurrentCombatState = PlayerCombatStates.Relaxed;
        }
    }

    /// <summary>Records the shot time and switches to Aiming or HipFire at once.</summary>
    private void HandleFire()
    {
        _lastShotTime = Time.time;
        CurrentCombatState = _playerCameraController.IsAiming
            ? PlayerCombatStates.Aiming
            : PlayerCombatStates.HipFire;
    }
}
