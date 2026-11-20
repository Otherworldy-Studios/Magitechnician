using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// The IDamageable on a character. Sends each hit to the shield first, and only the leftover reaches health.
/// Also runs damage over time. Put it on the object that has Health and, optionally, ShieldSystem.
/// An entity without a ShieldSystem takes all damage to health.
/// </summary>
public class DamageReceiver : MonoBehaviour, IDamageable
{
    [Tooltip("Found on this object if empty.")]
    [SerializeField] private Health _health;
    [Tooltip("Optional. Found on this object if empty.")]
    [SerializeField] private ShieldSystem _shield;

    // Damage over time. TakeDamageOverTime sets these, and Update spreads the total across the duration.
    private DamageInfo _currentDamageOverTime; // The original hit, so each tick keeps its source and type.
    private float _damageTimer;                // Seconds left.
    private float _damageDuration;             // Total seconds.
    private float _damageAccumulator;          // Fractional damage not applied yet, because damage is whole numbers.

    /// <summary>Finds Health and ShieldSystem on this object if they aren't assigned.</summary>
    private void Awake()
    {
        if (!_health)
            _health = GetComponent<Health>();

        if (!_shield)
            _shield = GetComponent<ShieldSystem>();
    }

    /// <summary>Applies damage over time in whole-point ticks, limiting the last tick to the time left.</summary>
    private void Update()
    {
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
                    DamageDirection = _currentDamageOverTime.DamageDirection,
                });
                _damageAccumulator -= damage;
            }

            _damageTimer -= deltaTime;
        }
    }

    /// <summary>Applies one hit, then runs the effect for its damage type. The type effects are empty for now.</summary>
    /// <param name="damageInfo">The hit. Projectile and EnemyMeleeHitbox build it.</param>
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (damageInfo.DamageAmount > 0)
            HandleDamage(damageInfo);

        switch (damageInfo.DamageType)
        {
            case DamageType.Electric:
                // Electric effect goes here.
                break;

            case DamageType.Fire:
                // Fire effect goes here, plus TakeDamageOverTime.
                break;

            case DamageType.Ice:
                // Ice effect goes here.
                break;

            case DamageType.Poison:
                // Poison effect goes here, plus TakeDamageOverTime.
                break;
        }
    }

    /// <summary>Lets the shield absorb what it can, then sends the leftover to health.</summary>
    private void HandleDamage(DamageInfo damageInfo)
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
    /// Deals damage over time. DamageAmount is the total over the duration, not damage per second.
    /// Replaces any damage over time already running.
    /// </summary>
    /// <param name="damageInfo">The hit, with the total damage.</param>
    /// <param name="time">Seconds to spread it over.</param>
    public void TakeDamageOverTime(DamageInfo damageInfo, float time)
    {
        _damageTimer = Mathf.Max(0f, time);
        _damageDuration = _damageTimer;
        _damageAccumulator = 0f;
        _currentDamageOverTime = damageInfo;
    }

    #region Debug
    // INTEGRATION: HealthDebug's F2 and F3 keys and ShieldTester's [ key became these Inspector buttons.
    // Damage goes through the shield first, so these test the shield too.

    /// <summary>Debug: deals one hit.</summary>
    [Button, DisableInEditorMode]
    private void DebugDamage(int amount = 25)
    {
        TakeDamage(DebugDamageInfo(amount));
    }

    /// <summary>Debug: deals damage over a number of seconds.</summary>
    [Button, DisableInEditorMode]
    private void DebugDamageOverTime(int amount = 50, float seconds = 10f)
    {
        TakeDamageOverTime(DebugDamageInfo(amount), seconds);
    }

    /// <summary>Debug: stops damage over time.</summary>
    [Button, DisableInEditorMode]
    private void DebugStopDamageOverTime()
    {
        _damageTimer = 0f;
    }

    /// <summary>Builds a physical hit from this object for the debug buttons.</summary>
    private DamageInfo DebugDamageInfo(int amount)
    {
        return new DamageInfo
        {
            DamageAmount = amount,
            DamageSource = gameObject,
            DamageDirection = transform.forward,
            DamageType = DamageType.Physical,
        };
    }
    #endregion
}
