using UnityEngine;

public class DamageReceiver : MonoBehaviour, IDamageable
{
    [SerializeField] private Health _health;
    [SerializeField] private ShieldSystem _shield;
    
    private DamageInfo _currentDamageOverTime;
    private float _damageTimer;
    private float _damageDuration;
    private float _damageAccumulator;

    private void Awake()
    {
        if(!_health)
            _health = GetComponent<Health>();

        if (!_shield)
            _shield = GetComponent<ShieldSystem>();
    }

    private void Update()
    {
        //Damaging over time
        if (_damageTimer > 0f)
        {
            float deltaTime = Mathf.Min(Time.deltaTime, _damageTimer);

            _damageAccumulator += (_currentDamageOverTime.DamageAmount / _damageDuration) * deltaTime;

            int damage = Mathf.FloorToInt(_damageAccumulator);

            if (damage > 0)
            {
                HandleDamage(new DamageInfo()
                {
                    DamageAmount = damage,
                    DamageSource = _currentDamageOverTime.DamageSource,
                    DamageType = _currentDamageOverTime.DamageType,
                    DamageDirection =  _currentDamageOverTime.DamageDirection,
                });
                _damageAccumulator -= damage;
            }

            _damageTimer -= deltaTime;
        }
    }
    
    public void TakeDamage(DamageInfo  damageInfo)
    {
        if(damageInfo.DamageAmount > 0)
            HandleDamage(damageInfo);
        
        switch (damageInfo.DamageType)
        {
            case DamageType.Electric:
                // Do electric logic
            break;
            
            case DamageType.Fire:
                // Do fire logic + DamageOverTime
            break;
            
            case DamageType.Ice:
                // Do Ice logic
            break;
            
            case DamageType.Poison:
                // Do poison logic + DamageOverTime
            break;
        }
    }
    
    private void HandleDamage(DamageInfo  damageInfo)
    {
        if (!_health && !_shield)
            return;
        
        DamageInfo info = damageInfo;
        
        if (_shield)
        {
            info.DamageAmount = _shield.AbsorbDamageAndReturnOverflow(damageInfo);
        }

        if (_health && info.DamageAmount > 0)
        {
            _health.TakeDamage(info);
        }
    }
    
    /// <summary>
    /// Applies damage over time.
    /// DamageAmount represents the total damage dealt over the specified duration,
    /// not damage per second. Replaces any currently active damage over time.
    /// </summary>
    public void TakeDamageOverTime(DamageInfo damageInfo, float time)
    {
        _damageTimer = Mathf.Max(0f, time);
        _damageDuration = _damageTimer;
        _damageAccumulator = 0f;
        _currentDamageOverTime = damageInfo;
    }
}
