
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shows the player's health as a fill and a number. The fill eases toward the new value.</summary>
// INTEGRATION: no longer needs a reference set in the Inspector. The HUD lives in the persistent UI, while the player
// is spawned per scene, so this connects to each player GameManager registers (GameManager.OnPlayerRegistered, new).
public class HealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health _health;
    [SerializeField] private Image _fill;
    [SerializeField] private Image _ghostFill;
    [SerializeField] private TMP_Text _healthCounter;

    [Header("Settings")]
    [SerializeField] private float _smoothDuration = 0.08f; // Seconds the fill takes to catch up.

    private float _visualHealth = 1f; // The fill shown now, from 0 to 1.
    private float _targetHealth = 1f; // The fill to ease toward: current health over max health.
    private float _velocity;          // Used by SmoothDamp between frames.

    // Reference to the player's Ghost Health.
    private GhostHealth _ghostHealth;

    /// <summary>Starts listening for player registration.</summary>
    // INTEGRATION: was a check that _health was assigned.
    private void Awake()
    {
        if (!_fill)
            Debug.LogError("Fill was not assigned in HealthBar");

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

        _healthCounter.text = _health.CurrentHealth.ToString("F0");
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
    /// Connects to the newly registered player's Health and shows its value at once. Runs on
    /// GameManager.OnPlayerRegistered, and from Start when a player was already registered.
    /// </summary>
    // INTEGRATION: replaces subscribing in OnEnable.
    private void HandlePlayerRegistered(PlayerRoot player)
    {
        Unbind();

        _health = player.PlayerHealth;
        if (_health == null)
            return;

        _health.OnDamaged += HandleHealthChange;
        _health.OnHealed += HandleHealthChange;

        // Find GhostHealth on the registered player.
        _ghostHealth =
            player.GetComponentInChildren<GhostHealth>(true);

        // Listen for recoverable Ghost Health changes.
        if (_ghostHealth != null)
        {
            _ghostHealth.OnGhostHealthChanged +=
                HandleGhostHealthChange;
        }

        // Snap to the new player's health with no smoothing.
        _targetHealth =
            (float)_health.CurrentHealth / _health.MaxHealth;

        _visualHealth = _targetHealth;
        _fill.fillAmount = _visualHealth;

        // Update white when the player connects.
        UpdateGhostFill();

        _healthCounter.text =
            _health.CurrentHealth.ToString("F0");
    }

    /// <summary>Disconnects from the current player, if any.</summary>
    private void Unbind()
    {
        if (_health != null)
        {
            _health.OnDamaged -= HandleHealthChange;
            _health.OnHealed -= HandleHealthChange;
        }

        if (_ghostHealth != null)
        {
            _ghostHealth.OnGhostHealthChanged -=
                HandleGhostHealthChange;
        }

        _ghostHealth = null;

        // FIX 3: Hide white when disconnecting.
        if (_ghostFill != null)
        {
            _ghostFill.fillAmount = 0f;
            _ghostFill.enabled = false;
        }

        _health = null;
    }

    /// <summary>Eases the fill toward the target.</summary>
    private void Update()
    {
        if (!Mathf.Approximately(_targetHealth, _visualHealth))
        {
            _visualHealth = Mathf.SmoothDamp(
                _visualHealth,
                _targetHealth,
                ref _velocity,
                _smoothDuration
            );

            _fill.fillAmount = _visualHealth;
        }

        // FIX 4: Make sure the white image always
        // matches the actual Ghost Health amount.
        UpdateGhostFill();
    }

    /// <summary>Sets the new target and number. Runs on Health.OnDamaged and OnHealed.</summary>
    private void HandleHealthChange(int amount, GameObject sender)
    {
        _targetHealth =
            (float)_health.CurrentHealth / _health.MaxHealth;

        _healthCounter.text =
            _health.CurrentHealth.ToString("F0");
    }

    /// <summary>Refreshes white whenever Ghost Health changes.</summary>
    private void HandleGhostHealthChange(float ghostHealth)
    {
        UpdateGhostFill();
    }

    // FIX 5:
    // Display the recoverable Ghost Health only.
    // Turn the white Image OFF when Ghost Health is zero.
    private void UpdateGhostFill()
    {
        if (_ghostFill == null)
            return;

        if (_health == null ||
            _ghostHealth == null ||
            _health.MaxHealth <= 0 ||
            _ghostHealth.GhostHealthAmount <= 0f)
        {
            // Completely hide the white Image.
            _ghostFill.fillAmount = 0f;
            _ghostFill.enabled = false;
            return;
        }

        // Show the white Image only when
        // actual recoverable Ghost Health exists.
        _ghostFill.enabled = true;

        float ghostHealthPercent =
            _ghostHealth.GhostHealthAmount /
            _health.MaxHealth;

        // White Image sits behind the red Image.
        _ghostFill.fillAmount = Mathf.Clamp01(
            _visualHealth + ghostHealthPercent
        );
    }
}