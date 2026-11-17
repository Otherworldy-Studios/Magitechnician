using System;
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

    /// <summary>Shield points left.</summary>
    [field: SerializeField] public float CurrentShield { get; private set; }
    /// <summary>True from the moment the shield hits 0 until it regenerates above 0.</summary>
    [field: SerializeField] public bool IsBroken { get; private set; }

    // INTEGRATION: new. The shield's values come from these stats on the player's Stats. PlayerRoot.AssignStats hands
    // this component the Stats through SetStats.
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

    // INTEGRATION: each value reads its stat every time, falling back to ShieldSettings, so a charm or consumable applies right away.
    public float MaxShield => ReadStat(_maxShieldStat, _settings.MaxShield);
    public float RegenRate => ReadStat(_regenRateStat, _settings.RegenRate);
    public float RegenDelay => ReadStat(_regenDelayStat, _settings.RegenDelay);
    public float BrokenRegenDelay => ReadStat(_brokenRegenDelayStat, _settings.BrokenRegenDelay);
    public float DamageReductionPercent => ReadStat(_damageReductionStat, _settings.DamageReductionPercent);

    /// <summary>Fires after the shield takes damage, with the shield left. ShieldBar listens to it.</summary>
    public event Action<float> OnShieldDamaged;
    /// <summary>Fires when the shield reaches 0.</summary>
    public event Action OnShieldBroken;
    /// <summary>Fires when the shield regenerates back to full.</summary>
    public event Action OnShieldRestored;
    /// <summary>Fires on every frame the shield regenerates.</summary>
    public event Action OnShieldHealed;

    [Tooltip("The player's Stats. PlayerRoot fills this in Awake if it's empty.")]
    [SerializeField] private Stats _playerStats;

    private float _regenTimer;     // Seconds left before regeneration starts.
    private Stats _subscribedTo;   // INTEGRATION: new. The Stats whose OnStatChanged this listens to, so it can unsubscribe.
    private bool _started;         // INTEGRATION: new. True after Start, so the subscription waits until the shield is filled.
    private float _lastMaxShield;  // INTEGRATION: new. MaxShield before the latest change, to work out how much it grew.

    /// <summary>Gives this component the Stats to read from, and listens to it once Start has run.</summary>
    // INTEGRATION: new. Called by PlayerRoot.AssignStats.
    public void SetStats(Stats playerStats)
    {
        if (_playerStats == null)
        {
            _playerStats = playerStats;
        }
        Subscribe();
    }

    /// <summary>Fills the shield and starts listening to stat changes.</summary>
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

    /// <summary>Stops listening to the Stats.</summary>
    private void OnDestroy()
    {
        if (_subscribedTo != null) _subscribedTo.OnStatChanged -= HandleStatChanged;
        _subscribedTo = null;
    }

    /// <summary>Moves the OnStatChanged subscription to the current Stats. Waits for Start, and does nothing if already subscribed.</summary>
    // INTEGRATION: new. Called from both SetStats and Start, because either can run first.
    private void Subscribe()
    {
        if (!_started || _subscribedTo == _playerStats) return;

        if (_subscribedTo != null) _subscribedTo.OnStatChanged -= HandleStatChanged;
        _subscribedTo = _playerStats;
        if (_subscribedTo != null) _subscribedTo.OnStatChanged += HandleStatChanged;
    }

    /// <summary>
    /// Follows changes to Stat_MaxShield. Growth is added to the current shield unless it's broken,
    /// and the current shield never ends up over the new max.
    /// </summary>
    private void HandleStatChanged(StatDefinition stat, float value)
    {
        if (stat != _maxShieldStat || _settings == null) return;

        float difference = value - _lastMaxShield;
        _lastMaxShield = value;

        if (difference > 0f && !IsBroken) CurrentShield += difference;
        CurrentShield = Mathf.Min(CurrentShield, value);
    }

    /// <summary>
    /// Waits for the regeneration delay, then restores shield over time and fires the matching events.
    /// </summary>
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
            return damageInfo.DamageAmount;
        }

        // INTEGRATION: the lower limit changed from 0 to 0.01, so a 100% reduction can't divide by zero below.
        float multiplier = Mathf.Clamp(1f - DamageReductionPercent / 100f, 0.01f, 1f);
        float modifiedDamage = damageInfo.DamageAmount * multiplier;

        if (modifiedDamage <= CurrentShield)
        {
            CurrentShield -= modifiedDamage;
            _regenTimer = RegenDelay;

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

            OnShieldDamaged?.Invoke(CurrentShield);
            OnShieldBroken?.Invoke();

            return leftoverDamage;
        }
    }

    /// <summary>Returns the stat's value, or the fallback when there's no Stats, no stat assigned, or the StatBlock doesn't list it.</summary>
    // INTEGRATION: new. The same helper every stat-reading script uses.
    private float ReadStat(StatDefinition stat, float fallback)
    {
        if (_playerStats == null || stat == null || !_playerStats.HasStat(stat))
            return fallback;

        return _playerStats.GetValue(stat);
    }
}
