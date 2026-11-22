using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shows the player's health as a fill and a number. The fill eases toward the new value.</summary>
// Connects to each player GameManager registers, because the HUD lives in the persistent UI while each scene has its
// own player.
public class HealthBar : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Set by HandlePlayerRegistered. Don't set it by hand.")]
    [SerializeField] private Health _health;
    [SerializeField] private Image _fill;
    [Tooltip("Sits behind Fill, and shows the Ghost Health that can still be recovered.")]
    [SerializeField] private Image _ghostFill;
    [SerializeField] private TMP_Text _healthCounter;

    [Header("Settings")]
    [Tooltip("Seconds the fill takes to catch up.")]
    [SerializeField] private float _smoothDuration = 0.08f;

    private float _visualHealth = 1f; // The fill shown now, from 0 to 1.
    private float _targetHealth = 1f; // The fill to ease toward: current health over max health.
    private float _velocity;          // Used by SmoothDamp between frames.
    private GhostHealth _ghostHealth;

    private void Awake()
    {
        if (!_fill)
            Debug.LogError("Fill was not assigned in HealthBar", this);

        GameManager.Instance.OnPlayerRegistered += HandlePlayerRegistered;
    }

    private void Start()
    {
        // If a player registered before this bar existed, connect to it now.
        if (GameManager.Instance.Player != null)
        {
            HandlePlayerRegistered(GameManager.Instance.Player.GetComponent<PlayerRoot>());
        }
    }

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
    private void HandlePlayerRegistered(PlayerRoot player)
    {
        Unbind();

        if (player == null) return;

        _health = player.PlayerHealth;
        if (_health == null) return;

        _health.OnDamaged += HandleHealthChange;
        _health.OnHealed += HandleHealthChange;
        _health.OnMaxHealthChanged += HandleHealthChange;

        _ghostHealth = player.GetComponentInChildren<GhostHealth>(true);
        if (_ghostHealth != null)
        {
            _ghostHealth.OnGhostHealthChanged += HandleGhostHealthChange;
        }

        // Snap to the new player's health with no smoothing.
        _targetHealth = HealthPercent();
        _visualHealth = _targetHealth;
        _fill.fillAmount = _visualHealth;

        UpdateGhostFill();
        _healthCounter.text = _health.CurrentHealth.ToString("F0");
    }

    /// <summary>Disconnects from the current player, if any.</summary>
    private void Unbind()
    {
        if (_health != null)
        {
            _health.OnDamaged -= HandleHealthChange;
            _health.OnHealed -= HandleHealthChange;
            _health.OnMaxHealthChanged -= HandleHealthChange;
        }

        if (_ghostHealth != null)
        {
            _ghostHealth.OnGhostHealthChanged -= HandleGhostHealthChange;
        }

        _ghostHealth = null;

        if (_ghostFill != null)
        {
            _ghostFill.fillAmount = 0f;
            _ghostFill.enabled = false;
        }

        _health = null;
    }

    private void Update()
    {
        if (!Mathf.Approximately(_targetHealth, _visualHealth))
        {
            _visualHealth = Mathf.SmoothDamp(_visualHealth, _targetHealth, ref _velocity, _smoothDuration);
            _fill.fillAmount = _visualHealth;
        }

        // The ghost fill sits on top of the eased health fill, so it follows it every frame.
        UpdateGhostFill();
    }

    /// <summary>Sets the new target and number. Runs on Health.OnDamaged, OnHealed, and OnMaxHealthChanged.</summary>
    private void HandleHealthChange(int currentHealth, GameObject sender)
    {
        if (_health == null) return;

        _targetHealth = HealthPercent();
        _healthCounter.text = _health.CurrentHealth.ToString("F0");
    }

    private void HandleGhostHealthChange(float ghostHealth)
    {
        UpdateGhostFill();
    }

    /// <summary>Shows the Ghost Health that can still be recovered, past the health fill. Hidden at 0.</summary>
    private void UpdateGhostFill()
    {
        if (_ghostFill == null) return;

        if (_health == null || _ghostHealth == null || _health.MaxHealth <= 0 || _ghostHealth.GhostHealthAmount <= 0f)
        {
            _ghostFill.fillAmount = 0f;
            _ghostFill.enabled = false;
            return;
        }

        _ghostFill.enabled = true;

        float ghostHealthPercent = _ghostHealth.GhostHealthAmount / _health.MaxHealth;
        _ghostFill.fillAmount = Mathf.Clamp01(_visualHealth + ghostHealthPercent);
    }

    private float HealthPercent()
    {
        return _health.MaxHealth > 0 ? (float)_health.CurrentHealth / _health.MaxHealth : 0f;
    }
}