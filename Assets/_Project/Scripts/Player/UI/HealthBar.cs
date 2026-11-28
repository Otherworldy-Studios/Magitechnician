using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MoreMountains.Feedbacks;

/// <summary>Shows the player's health as a fill and a number. The fill eases toward the new value.</summary>
// Connects to each player GameManager registers, because the HUD lives in the persistent UI while each scene has its
// own player. BossHealthBar and EnemyHealthBar inherit this, turn off BindsToPlayer, and call Bind themselves.
public class HealthBar : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Health this bar shows. Set when the bar binds. Don't set it by hand.")]
    [SerializeField] private Health _health;
    [SerializeField] private Image _fill;
    [Tooltip("Sits behind Fill, and shows the Ghost Health that can still be recovered. Optional.")]
    [SerializeField] private Image _ghostFill;
    [Tooltip("Optional.")]
    [SerializeField] private TMP_Text _healthCounter;

    [Header("Settings")]
    [Tooltip("Seconds the fill takes to catch up.")]
    [SerializeField] private float _smoothDuration = 0.08f;
    
    [SerializeField] private MMF_Player _damageFeedback;

    private float _visualHealth = 1f; // The fill shown now, from 0 to 1.
    private float _targetHealth = 1f; // The fill to ease toward: current health over max health.
    private float _velocity;          // Used by SmoothDamp between frames.
    private GhostHealth _ghostHealth;

    protected Health BoundHealth => _health;
    protected float VisualHealth => _visualHealth;
    protected virtual bool BindsToPlayer => true;

    protected virtual void Awake()
    {
        if (!_fill)
            Debug.LogError("Fill was not assigned in HealthBar", this);

        if (BindsToPlayer)
            GameManager.Instance.OnPlayerRegistered += HandlePlayerRegistered;
    }

    protected virtual void Start()
    {
        // If a player registered before this bar existed, connect to it now.
        if (BindsToPlayer && GameManager.Instance.Player != null)
        {
            HandlePlayerRegistered(GameManager.Instance.Player.GetComponent<PlayerRoot>());
        }
    }

    protected virtual void OnDestroy()
    {
        if (BindsToPlayer && GameManager.HasInstance)
        {
            GameManager.Instance.OnPlayerRegistered -= HandlePlayerRegistered;
        }

        Unbind();
    }

    protected virtual void Update()
    {
        if (_health == null) return;

        // Health sets MaxHealth in its own Start, which can run after Bind, so the target is read every frame.
        _targetHealth = HealthPercent();

        if (!Mathf.Approximately(_targetHealth, _visualHealth))
        {
            _visualHealth = Mathf.SmoothDamp(_visualHealth, _targetHealth, ref _velocity, _smoothDuration);
            _fill.fillAmount = _visualHealth;
        }

        // The ghost fill sits on top of the eased health fill, so it follows it every frame.
        UpdateGhostFill();
    }

    /// <summary>Starts showing this Health, snapping to its value with no smoothing. Null leaves the bar empty.</summary>
    protected void Bind(Health health)
    {
        Unbind();

        _health = health;
        if (_health == null) return;

        _health.OnDamaged += HandleHealthChange;
        _health.OnHealed += HandleHealthChange;
        _health.OnMaxHealthChanged += HandleHealthChange;

        _targetHealth = HealthPercent();
        _visualHealth = _targetHealth;
        _fill.fillAmount = _visualHealth;

        UpdateCounter();
    }

    protected void Unbind()
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

    /// <summary>Runs after the bound Health is damaged, healed, or has its max changed, with the new share of max health.</summary>
    protected virtual void OnHealthChanged(float healthPercent) { }

    /// <summary>
    /// Connects to the newly registered player's Health and Ghost Health, and shows their values at once. Runs on
    /// GameManager.OnPlayerRegistered, and from Start when a player was already registered.
    /// </summary>
    private void HandlePlayerRegistered(PlayerRoot player)
    {
        if (player == null)
        {
            Unbind();
            return;
        }

        Bind(player.PlayerHealth);
        if (_health == null) return;

        _ghostHealth = player.GetComponentInChildren<GhostHealth>(true);
        if (_ghostHealth != null)
        {
            _ghostHealth.OnGhostHealthChanged += HandleGhostHealthChange;
        }

        UpdateGhostFill();
    }

    private void HandleHealthChange(int currentHealth, GameObject sender)
    {
        if (_health == null) return;

        _targetHealth = HealthPercent();
        UpdateCounter();
        OnHealthChanged(_targetHealth);

        if (_damageFeedback != null)
        {
            _damageFeedback.PlayFeedbacks();
        }
    }

    private void HandleGhostHealthChange(float ghostHealth)
    {
        UpdateGhostFill();
    }

    private void UpdateCounter()
    {
        if (_healthCounter != null && _health != null)
        {
            _healthCounter.text = _health.CurrentHealth.ToString("F0");
        }
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