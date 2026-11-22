using System;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// A shield that absorbs damage before health. Incoming damage is reduced by a percentage, the shield takes what it can,
/// and the rest goes back to DamageReceiver for health. After a delay without damage it regenerates, and a broken shield
/// waits a longer delay. Every value comes from a stat, with ShieldSettings as the fallback.
/// </summary>
public class ShieldSystem : MonoBehaviour
{
    [Tooltip("Fallback values, used for any shield stat that isn't assigned. The shield does nothing without this asset.")]
    [SerializeField] private ShieldSettings _settings;

    [Tooltip("The player's Stats. PlayerRoot fills this through SetStats if it's empty.")]
    [SerializeField] private Stats _playerStats;

    // The shield's values come from these stats on the player's Stats.
    [Header("Stats")]
    [Tooltip("Stat_MaxShield.")]
    [SerializeField] private StatDefinition _maxShieldStat;
    [Tooltip("Stat_ShieldRegenRate, in points per second.")]
    [SerializeField] private StatDefinition _regenRateStat;
    [Tooltip("Stat_ShieldRegenDelay, in seconds after damage.")]
    [SerializeField] private StatDefinition _regenDelayStat;
    [Tooltip("Stat_ShieldBrokenRegenDelay, in seconds after the shield breaks.")]
    [SerializeField] private StatDefinition _brokenRegenDelayStat;
    [Tooltip("Stat_ShieldDamageReduction, in percent.")]
    [SerializeField] private StatDefinition _damageReductionStat;

    private float _regenTimer;     // Seconds left before regeneration starts.
    private bool _isRegenerating;  // True while regenerating, so OnShieldRegenStarted fires once per recharge.
    private Stats _subscribedTo;
    private bool _started;
    private float _lastMaxShield;  // MaxShield before the latest change, to work out how much it grew.

    /// <summary>Fires after the shield takes damage, with the shield left. ShieldBar listens to it.</summary>
    public event Action<float> OnShieldDamaged;
    /// <summary>Fires when the shield reaches 0.</summary>
    public event Action OnShieldBroken;
    /// <summary>Fires when the shield gets back to full.</summary>
    public event Action OnShieldRestored;
    /// <summary>Fires on every frame the shield regenerates, and on every Restore.</summary>
    public event Action OnShieldHealed;
    /// <summary>Fires once when the shield starts regenerating after the delay. PlayerAudio plays the recharge sound from it.</summary>
    public event Action OnShieldRegenStarted;
    /// <summary>Fires after MaxShield changes, such as from a charm. ShieldBar updates from it.</summary>
    public event Action OnMaxShieldChanged;

    /// <summary>Shield points left.</summary>
    [field: SerializeField] public float CurrentShield { get; private set; }
    /// <summary>True from the moment the shield hits 0 until it regenerates or is restored.</summary>
    [field: SerializeField] public bool IsBroken { get; private set; }

    // Each value reads its stat every time, so a charm or consumable applies right away.
    public float MaxShield => ReadStat(_maxShieldStat, _settings.MaxShield);
    public float RegenRate => ReadStat(_regenRateStat, _settings.RegenRate);
    public float RegenDelay => ReadStat(_regenDelayStat, _settings.RegenDelay);
    public float BrokenRegenDelay => ReadStat(_brokenRegenDelayStat, _settings.BrokenRegenDelay);
    public float DamageReductionPercent => ReadStat(_damageReductionStat, _settings.DamageReductionPercent);

    /// <summary>Gives this component the Stats to read from, and listens to it once Start has run. Called by PlayerRoot.</summary>
    public void SetStats(Stats playerStats)
    {
        if (_playerStats == null)
        {
            _playerStats = playerStats;
        }

        Subscribe();
    }

    private void Start()
    {
        if (_settings != null)
        {
            CurrentShield = MaxShield;
            _lastMaxShield = MaxShield;
        }

        _started = true;
        Subscribe();
    }

    private void OnDestroy()
    {
        if (_subscribedTo != null) _subscribedTo.OnStatChanged -= HandleStatChanged;
        _subscribedTo = null;
    }

    private void Update()
    {
        if (_settings == null) return;

        float maxShield = MaxShield; // Read once per frame, since it reads the stat.

        if (_regenTimer > 0)
        {
            _regenTimer -= Time.deltaTime;
        }
        else if (CurrentShield < maxShield)
        {
            if (!_isRegenerating)
            {
                _isRegenerating = true;
                OnShieldRegenStarted?.Invoke();
            }

            float previousShield = CurrentShield;
            CurrentShield += RegenRate * Time.deltaTime;
            CurrentShield = Mathf.Clamp(CurrentShield, 0, maxShield);

            if (CurrentShield > previousShield)
            {
                OnShieldHealed?.Invoke();
            }

            if (CurrentShield > 0)
            {
                IsBroken = false;
            }

            if (previousShield < maxShield && CurrentShield >= maxShield)
            {
                OnShieldRestored?.Invoke();
            }
        }
    }

    /// <summary>
    /// Moves the OnStatChanged subscription to the current Stats. Waits for Start, and does nothing if already
    /// subscribed. Called from both SetStats and Start, because either can run first.
    /// </summary>
    private void Subscribe()
    {
        if (!_started || _subscribedTo == _playerStats) return;

        if (_subscribedTo != null) _subscribedTo.OnStatChanged -= HandleStatChanged;
        _subscribedTo = _playerStats;
        if (_subscribedTo != null) _subscribedTo.OnStatChanged += HandleStatChanged;
    }

    /// <summary>
    /// Follows changes to Stat_MaxShield. Growth is added to the current shield unless it's broken,
    /// and the current shield never ends up over the new max. Fires OnMaxShieldChanged so the HUD updates.
    /// </summary>
    private void HandleStatChanged(StatDefinition stat, float value)
    {
        if (stat != _maxShieldStat || _settings == null) return;

        float difference = value - _lastMaxShield;
        if (Mathf.Approximately(difference, 0f)) return;

        _lastMaxShield = value;

        if (difference > 0f && !IsBroken) CurrentShield += difference;
        CurrentShield = Mathf.Min(CurrentShield, value);

        OnMaxShieldChanged?.Invoke();
    }

    /// <summary>
    /// Reduces the damage by the damage reduction percentage, takes what the shield can, and returns the rest
    /// for health. A broken or empty shield takes nothing and restarts the broken delay.
    /// </summary>
    /// <param name="damageInfo">The incoming hit, from DamageReceiver.</param>
    /// <returns>The damage left for health, in the original, unreduced amount.</returns>
    public int AbsorbDamageAndReturnOverflow(DamageInfo damageInfo)
    {
        if (_settings == null || damageInfo.DamageAmount <= 0) return damageInfo.DamageAmount;

        if (CurrentShield <= 0 || IsBroken)
        {
            _regenTimer = BrokenRegenDelay;
            _isRegenerating = false; // The next recharge plays its sound again.
            return damageInfo.DamageAmount;
        }

        // The lower limit is 0.01, so a 100% reduction can't divide by zero below.
        float multiplier = Mathf.Clamp(1f - DamageReductionPercent / 100f, 0.01f, 1f);
        float modifiedDamage = damageInfo.DamageAmount * multiplier;

        if (modifiedDamage <= CurrentShield)
        {
            CurrentShield -= modifiedDamage;
            _regenTimer = RegenDelay;
            _isRegenerating = false;

            OnShieldDamaged?.Invoke(CurrentShield);
            return 0;
        }
        else
        {
            // Convert the shield that was left back to its unreduced amount, then pass the rest of the hit to health.
            float shieldAbsorbedRaw = CurrentShield / multiplier;
            int leftoverDamage = Mathf.RoundToInt(damageInfo.DamageAmount - shieldAbsorbedRaw);

            CurrentShield = 0;
            IsBroken = true;
            _regenTimer = BrokenRegenDelay;
            _isRegenerating = false;

            OnShieldDamaged?.Invoke(CurrentShield);
            OnShieldBroken?.Invoke();

            return leftoverDamage;
        }
    }

    /// <summary>
    /// Adds shield right away, up to the max, and un-breaks the shield. Leech Rounds calls it on every hit.
    /// Also an Inspector button.
    /// </summary>
    [Button]
    public void Restore(float amount)
    {
        if (Mathf.Approximately(CurrentShield, MaxShield))
            return;

        float temp = CurrentShield + amount;
        CurrentShield = Mathf.Min(temp, MaxShield);
        OnShieldHealed?.Invoke();

        if (Mathf.Approximately(CurrentShield, MaxShield))
            OnShieldRestored?.Invoke();

        IsBroken = false;
    }

    /// <summary>Returns the stat's value, or the fallback when there's no Stats, no stat assigned, or the StatBlock doesn't list it.</summary>
    private float ReadStat(StatDefinition stat, float fallback)
    {
        if (_playerStats == null || stat == null || !_playerStats.HasStat(stat))
            return fallback;

        return _playerStats.GetValue(stat);
    }
}