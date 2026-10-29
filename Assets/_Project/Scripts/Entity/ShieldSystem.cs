using System;
using TMPro;
using UnityEngine;

public class ShieldSystem : MonoBehaviour
{
    [SerializeField] private ShieldSettings _settings;

    [field: SerializeField] public float CurrentShield { get; private set; }
    [field: SerializeField] public bool IsBroken { get; private set; }
    public float MaxShield =>  _settings.maxShield;

    public event Action<float> OnShieldDamaged;
    public event Action OnShieldBroken;
    public event Action OnShieldRestored;
    public event Action OnShieldHealed;

    private float timer;

    void Awake()
    {
        if (_settings != null)
        {
            CurrentShield = _settings.maxShield;
        }
    }

    /// <summary>
    /// Waits for the regeneration delay, then restores shield over time.
    /// </summary>
    void Update()
    {
        if (_settings == null) return;

        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }
        else if (CurrentShield < _settings.maxShield)
        {
            float previousShield = CurrentShield;
            CurrentShield += _settings.regenRate * Time.deltaTime;
            CurrentShield = Mathf.Clamp(CurrentShield, 0, _settings.maxShield);
            
            if (CurrentShield > previousShield)
            {
                OnShieldHealed?.Invoke();
            }

            if (CurrentShield > 0)
            {
                IsBroken = false;
            }

            if (previousShield < _settings.maxShield && CurrentShield >= _settings.maxShield)
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
            timer = _settings.brokenRegenDelay;
            return damageInfo.DamageAmount;
        }

        float multiplier = Mathf.Clamp01(1f - _settings.damageReductionPercent / 100f);
        float modifiedDamage = damageInfo.DamageAmount * multiplier;

        if (modifiedDamage <= CurrentShield)
        {
            CurrentShield -= modifiedDamage;
            timer = _settings.regenDelay;

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
            timer = _settings.brokenRegenDelay;

            OnShieldDamaged?.Invoke(CurrentShield);
            OnShieldBroken?.Invoke();

            return leftoverDamage;
        }
    }
}