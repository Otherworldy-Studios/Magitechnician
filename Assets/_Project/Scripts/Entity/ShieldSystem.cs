using System;
using UnityEngine;


public class ShieldSystem : MonoBehaviour
{
    [SerializeField] private ShieldSettings _settings;

    [field: SerializeField] public float CurrentShield { get; private set; }
    [field: SerializeField] public bool IsBroken { get; private set; }

    
    [Header("Stats")]                                                      
    [SerializeField] private StatDefinition _maxShieldStat;               
    [SerializeField] private StatDefinition _regenRateStat;                
    [SerializeField] private StatDefinition _regenDelayStat;              
    [SerializeField] private StatDefinition _brokenRegenDelayStat;        
    [SerializeField] private StatDefinition _damageReductionStat;          

   
    public float MaxShield => ReadStat(_maxShieldStat, _settings.maxShield);                          
    public float RegenRate => ReadStat(_regenRateStat, _settings.regenRate);                        
    public float RegenDelay => ReadStat(_regenDelayStat, _settings.regenDelay);                      
    public float BrokenRegenDelay => ReadStat(_brokenRegenDelayStat, _settings.brokenRegenDelay);    
    public float DamageReductionPercent => ReadStat(_damageReductionStat, _settings.damageReductionPercent); 

    public event Action<float> OnShieldDamaged;
    public event Action OnShieldBroken;
    public event Action OnShieldRestored;
    public event Action OnShieldHealed;

    private float timer;
    [SerializeField] private Stats _playerStats;


    public void SetStats(Stats playerStats)
    {
        if (_playerStats == null)
        {
            _playerStats = playerStats;
        }
        Subscribe(); 
    }

    
    void Start()
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

    
    private Stats _subscribedTo;
    private bool _started;
    private void Subscribe()
    {
        if (!_started || _subscribedTo == _playerStats) return;

        if (_subscribedTo != null) _subscribedTo.OnStatChanged -= HandleStatChanged;
        _subscribedTo = _playerStats;
        if (_subscribedTo != null) _subscribedTo.OnStatChanged += HandleStatChanged;
    }

    
    private float _lastMaxShield;
    private void HandleStatChanged(StatDefinition stat, float value)
    {
        if (stat != _maxShieldStat || _settings == null) return;

        float difference = value - _lastMaxShield;
        _lastMaxShield = value;

        if (difference > 0f && !IsBroken) CurrentShield += difference;
        CurrentShield = Mathf.Min(CurrentShield, value);
    }

    /// <summary>
    /// Waits for the regeneration delay, then restores shield over time.
    /// </summary>
    void Update()
    {
        if (_settings == null) return;

        float maxShield = MaxShield; 

        if (timer > 0)
        {
            timer -= Time.deltaTime;
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
    /// Applies shield damage reduction, absorbs available damage,
    /// and returns any remaining damage for the damage receiver.
    /// </summary>
    public int AbsorbDamageAndReturnOverflow(DamageInfo damageInfo)
    {
        if (_settings == null || damageInfo.DamageAmount <= 0) return damageInfo.DamageAmount;

        if (CurrentShield <= 0 || IsBroken)
        {
            timer = BrokenRegenDelay; 
            return damageInfo.DamageAmount;
        }

       
        float multiplier = Mathf.Clamp(1f - DamageReductionPercent / 100f, 0.01f, 1f);
        float modifiedDamage = damageInfo.DamageAmount * multiplier;

        if (modifiedDamage <= CurrentShield)
        {
            CurrentShield -= modifiedDamage;
            timer = RegenDelay; 

            OnShieldDamaged?.Invoke(CurrentShield);
            return 0;
        }
        else
        {
            // Convert absorbed shield damage back to its original amount before reduction.
            float shieldAbsorbedRaw = CurrentShield / multiplier;
            int leftoverDamage = Mathf.RoundToInt(damageInfo.DamageAmount - shieldAbsorbedRaw);

            CurrentShield = 0;
            IsBroken = true;
            timer = BrokenRegenDelay; 

            OnShieldDamaged?.Invoke(CurrentShield);
            OnShieldBroken?.Invoke();

            return leftoverDamage;
        }
    }

  
    private float ReadStat(StatDefinition stat, float fallback)
    {
        if (_playerStats == null || stat == null || !_playerStats.HasStat(stat))
            return fallback;

        return _playerStats.GetValue(stat);
    }
}