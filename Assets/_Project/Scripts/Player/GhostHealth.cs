using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Contested health. Part of the damage the player takes becomes Ghost Health, which drains away over a few seconds.
/// Damage the player's gun deals during that time turns some of it back into real health.
/// </summary>
[DefaultExecutionOrder(100)]
public class GhostHealth : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Found on this object, its parents, or its children if empty.")]
    [SerializeField] private Health _health;

    [Tooltip("The player's gun. Damage it deals recovers Ghost Health. Found through PlayerRoot if empty.")]
    [SerializeField] private Gun _ghostRecoverySource;

    [Tooltip("Only used by the debug buttons.")]
    [SerializeField] private DamageReceiver _damageReceiver;

    [Header("Ghost Health Settings")]
    [SerializeField] private bool _useContestedHealth = true;

    [Tooltip("Fraction of the damage taken that becomes Ghost Health.")]
    [SerializeField, Range(0f, 1f)] private float _ghostHealthPercent = 0.4f;

    [Tooltip("Seconds Ghost Health takes to drain away after the last hit.")]
    [SerializeField, Min(0.1f)] private float _ghostDrainTime = 5f;

    [Tooltip("Fraction of the damage dealt to enemies that turns Ghost Health back into health.")]
    [SerializeField, Range(0f, 1f)] private float _ghostRecoveryPercent = 0.2f;

    private int _previousHealth;
    private float _ghostTimeRemaining;
    private float _pendingRecovery; // Recovery below 1 HP, kept until it adds up to a whole point.

    public float GhostHealthAmount { get; private set; }

    public event Action<float> OnGhostHealthChanged;

    /// <summary>Fires right before health is restored from Ghost Health, with the amount. PlayerAudio plays the recover sound from it.</summary>
    public event Action<int> OnGhostRecovered;

    private void Awake()
    {
        if (_health == null)
        {
            _health = GetComponent<Health>();

            if (_health == null)
                _health = GetComponentInParent<Health>();

            if (_health == null)
                _health = GetComponentInChildren<Health>();
        }

        if (_health == null)
        {
            Debug.LogError("GhostHealth: No Health component assigned!", this);
            enabled = false;
        }

        if (_ghostRecoverySource == null)
        {
            PlayerRoot root = GetComponentInParent<PlayerRoot>();
            if (root != null)
                _ghostRecoverySource = root.Gun != null ? root.Gun : root.GetComponentInChildren<Gun>();
        }
    }

    private void Start()
    {
        if (_health == null)
            return;

        // Health sets its starting HP in Start, which runs before this one because of the execution order.
        _previousHealth = _health.CurrentHealth;
    }

    private void OnEnable()
    {
        if (_health != null)
        {
            _health.OnDamaged += HandleDamage;
            _health.OnHealed += HandleHealing;
            _health.OnDied += HandleDeath;
        }

        if (_ghostRecoverySource != null)
            _ghostRecoverySource.OnDamageDealt += HandleDamageDealt;
    }

    private void OnDisable()
    {
        if (_health != null)
        {
            _health.OnDamaged -= HandleDamage;
            _health.OnHealed -= HandleHealing;
            _health.OnDied -= HandleDeath;
        }

        if (_ghostRecoverySource != null)
            _ghostRecoverySource.OnDamageDealt -= HandleDamageDealt;
    }

    private void Update()
    {
        if (!_useContestedHealth)
        {
            SetGhostHealth(0f);
            return;
        }

        if (_health == null || _health.CurrentHealth <= 0 || GhostHealthAmount <= 0f)
            return;

        DrainGhostHealth();
    }

    /// <summary>Turns part of the damage taken into Ghost Health, and restarts the drain timer.</summary>
    private void HandleDamage(int currentHealth, GameObject player)
    {
        int damageTaken = Mathf.Max(0, _previousHealth - currentHealth);
        _previousHealth = currentHealth;

        if (!_useContestedHealth || damageTaken <= 0)
            return;

        float ghostGained = damageTaken * _ghostHealthPercent;
        float missingHealth = _health.MaxHealth - currentHealth;

        SetGhostHealth(Mathf.Min(GhostHealthAmount + ghostGained, missingHealth));

        if (GhostHealthAmount > 0f)
        {
            _ghostTimeRemaining = _ghostDrainTime;
        }
    }

    /// <summary>Keeps Ghost Health from covering more than the health that's still missing.</summary>
    private void HandleHealing(int currentHealth, GameObject player)
    {
        _previousHealth = currentHealth;
        SetGhostHealth(Mathf.Min(GhostHealthAmount, _health.MaxHealth - currentHealth));
    }

    private void HandleDeath(GameObject player)
    {
        _previousHealth = 0;
        SetGhostHealth(0f);
    }

    /// <summary>Lowers Ghost Health evenly, so it reaches zero when the drain timer runs out.</summary>
    private void DrainGhostHealth()
    {
        if (_ghostTimeRemaining <= 0f)
        {
            SetGhostHealth(0f);
            return;
        }

        float delta = Mathf.Min(Time.deltaTime, _ghostTimeRemaining);
        float t = delta / _ghostTimeRemaining;
        SetGhostHealth(Mathf.Lerp(GhostHealthAmount, 0f, t));

        _ghostTimeRemaining = Mathf.Max(0f, _ghostTimeRemaining - delta);

        if (_ghostTimeRemaining <= 0f)
            SetGhostHealth(0f);
    }

    // Runs on Gun.OnDamageDealt, so only the player's own bullets recover Ghost Health.
    private void HandleDamageDealt(GameObject target, DamageInfo damageInfo)
    {
        if (GhostHealthAmount <= 0f || _ghostTimeRemaining <= 0f)
            return;

        RecoverGhostHealth(damageInfo.DamageAmount);
    }

    /// <summary>
    /// Turns a share of the damage dealt back into real health, up to the Ghost Health left. Returns the HP restored.
    /// </summary>
    public int RecoverGhostHealth(int damageDealt)
    {
        if (!_useContestedHealth ||
            _health == null ||
            _health.CurrentHealth <= 0 ||
            damageDealt <= 0 ||
            _ghostTimeRemaining <= 0f ||
            GhostHealthAmount <= 0f)
            return 0;

        // For example, 20 damage dealt at 0.2 recovers 4 HP.
        _pendingRecovery += damageDealt * _ghostRecoveryPercent;

        int recovery = Mathf.FloorToInt(
            Mathf.Min(_pendingRecovery,
                      GhostHealthAmount,
                      _health.MaxHealth - _health.CurrentHealth)
        );

        if (recovery <= 0)
            return 0;

        _pendingRecovery -= recovery;

        // Uses up only what was recovered. The rest stays, so more hits can keep recovering until it drains away.
        // Lowered before the heal, so HandleHealing never sees Ghost Health covering more than the missing health.
        SetGhostHealth(GhostHealthAmount - recovery);

        // Before the heal, so PlayerAudio can skip the normal heal sound.
        OnGhostRecovered?.Invoke(recovery);

        _health.RegenHealth(recovery);

        return recovery;
    }

    /// <summary>Sets Ghost Health, never below zero. Reaching zero also stops the drain timer and pending recovery.</summary>
    private void SetGhostHealth(float amount)
    {
        float newAmount = Mathf.Max(0f, amount);

        if (newAmount <= 0f)
        {
            _ghostTimeRemaining = 0f;
            _pendingRecovery = 0f;
        }

        if (GhostHealthAmount == newAmount)
            return;

        GhostHealthAmount = newAmount;
        OnGhostHealthChanged?.Invoke(GhostHealthAmount);
    }

    #region Debug

    [Button, DisableInEditorMode]
    private void DebugGhostHealth()
    {
        if (_damageReceiver == null)
        {
            Debug.LogWarning("Assign DamageReceiver in the Inspector.", this);
            return;
        }

        DamageInfo damage = new DamageInfo
        {
            DamageAmount = 30,
            DamageSource = gameObject,
            DamageDirection = Vector3.zero,
            DamageType = DamageType.Physical
        };

        _damageReceiver.TakeDamage(damage);
        Debug.Log("Debug: Player received damage.");
        PrintHealth();
    }

    [Button, DisableInEditorMode]
    private void DebugRecoverGhostHealth()
    {
        int recovered = RecoverGhostHealth(20);
        Debug.Log("Debug: Recovered " + recovered + " HP.");
        PrintHealth();
    }

    private void PrintHealth()
    {
        if (_health == null)
            return;

        Debug.Log("Real HP: " + _health.CurrentHealth + " | Ghost HP: " + GhostHealthAmount);
    }

    #endregion
}