using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shows the player's shield as a fill and a number. The fill eases toward the new value.</summary>
// INTEGRATION: no longer needs a reference set in the Inspector. The HUD lives in the persistent UI, while the player
// is spawned per scene, so this connects to each player GameManager registers (GameManager.OnPlayerRegistered, new).
public class ShieldBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShieldSystem _shieldSystem;
    [SerializeField] private Image _fill;
    [SerializeField] private TMP_Text _shieldCounter;
    
    [Header("Settings")]
    [SerializeField] private float _smoothDuration = 0.08f; // Seconds the fill takes to catch up.

    private float _visualShield = 1f; // The fill shown now, from 0 to 1.
    private float _targetShield = 1f; // The fill to ease toward: current shield over max shield.
    private float _velocity;          // Used by SmoothDamp between frames.

    /// <summary>Starts listening for player registration.</summary>
    // INTEGRATION: was a check that _shieldSystem was assigned.
    private void Awake()
    {
        if (!_fill)
            Debug.LogError("No Fill found in ShieldBar");

        GameManager.Instance.OnPlayerRegistered += HandlePlayerRegistered;
    }

    /// <summary>Connects to a player that registered before this bar existed.</summary>
    private void Start()
    {
        // If a player registered before this bar existed, connect to it now.
        if (GameManager.Instance.Player != null)
        {
            HandlePlayerRegistered(GameManager.Instance.Player.GetComponent<PlayerRoot>());
        }
    }

    /// <summary>Stops listening to GameManager and the player.</summary>
    private void OnDestroy()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnPlayerRegistered -= HandlePlayerRegistered;
        }
        Unbind();
    }

    /// <summary>
    /// Connects to the newly registered player's ShieldSystem and shows its value at once. Runs on
    /// GameManager.OnPlayerRegistered, and from Start when a player was already registered.
    /// </summary>
    // INTEGRATION: replaces subscribing in OnEnable.
    private void HandlePlayerRegistered(PlayerRoot player)
    {
        Unbind();

        _shieldSystem = player.Shield;
        if (_shieldSystem == null)
            return;

        _shieldSystem.OnShieldDamaged += HandleShieldDamaged;
        _shieldSystem.OnShieldHealed += HandleShieldHealed;
        _shieldSystem.OnShieldBroken += HandleShieldBroken;
        _shieldSystem.OnShieldRestored += HandleShieldRestored;

        // Snap to the new player's shield with no smoothing.
        _targetShield = _shieldSystem.CurrentShield / _shieldSystem.MaxShield;
        _visualShield = _targetShield;
        _fill.fillAmount = _visualShield;
        _shieldCounter.text = _shieldSystem.CurrentShield.ToString("F0");
    }

    /// <summary>Disconnects from the current player, if any.</summary>
    private void Unbind()
    {
        if (_shieldSystem != null)
        {
            _shieldSystem.OnShieldDamaged -= HandleShieldDamaged;
            _shieldSystem.OnShieldHealed -= HandleShieldHealed;
            _shieldSystem.OnShieldBroken -= HandleShieldBroken;
            _shieldSystem.OnShieldRestored -= HandleShieldRestored;
        }
        _shieldSystem = null;
    }

    /// <summary>Eases the fill toward the target.</summary>
    private void Update()
    {
        if (!Mathf.Approximately(_targetShield, _visualShield))
        {
            _visualShield = Mathf.SmoothDamp(
                _visualShield,
                _targetShield,
                ref _velocity,
                _smoothDuration
            );

            _fill.fillAmount = _visualShield;
        }
    }

    /// <summary>Sets the new target and number. Runs on ShieldSystem.OnShieldDamaged.</summary>
    private void HandleShieldDamaged(float damage)
    {
        _targetShield = _shieldSystem.CurrentShield / _shieldSystem.MaxShield;
        _shieldCounter.text = _shieldSystem.CurrentShield.ToString("F0");
    }

    /// <summary>Sets the new target and number. Runs on ShieldSystem.OnShieldHealed.</summary>
    private void HandleShieldHealed()
    {
        _targetShield = _shieldSystem.CurrentShield / _shieldSystem.MaxShield;
        _shieldCounter.text = _shieldSystem.CurrentShield.ToString("F0");
    }

    /// <summary>Empty. For an effect when the shield comes back. Runs on ShieldSystem.OnShieldRestored.</summary>
    private void HandleShieldRestored()
    {
    }

    /// <summary>Empty. For an effect when the shield breaks. Runs on ShieldSystem.OnShieldBroken.</summary>
    private void HandleShieldBroken()
    {
    }
}
