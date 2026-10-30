using System;
using UnityEngine;

public class ShieldSystem : MonoBehaviour
{
    [SerializeField] private ShieldSettings settings;

    public float CurrentShield { get; private set; }
    public bool IsBroken { get; private set; }

    public event Action<float> OnShieldDamaged;
    public event Action OnShieldBroken;
    public event Action OnShieldRestored;

    private float timer;

    void Start()
    {
        if (settings != null)
        {
            CurrentShield = settings.maxShield;
        }
    }

    /// <summary>
    /// Waits for the regeneration delay, then restores shield over time.
    /// </summary>
    void Update()
    {
        if (settings == null) return;

        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }
        else if (CurrentShield < settings.maxShield)
        {
            float previousShield = CurrentShield;
            CurrentShield += settings.regenRate * Time.deltaTime;
            CurrentShield = Mathf.Clamp(CurrentShield, 0, settings.maxShield);

            if (CurrentShield > 0)
            {
                IsBroken = false;
            }

            if (previousShield < settings.maxShield && CurrentShield >= settings.maxShield)
            {
                OnShieldRestored?.Invoke();
            }
        }
    }

    /// <summary>
    /// Applies shield damage reduction, absorbs available damage,
    /// and returns any remaining damage for the damage receiver.
    /// </summary>
    public float AbsorbDamageAndReturnOverflow(DamageInfo damageInfo)
    {
        if (settings == null || damageInfo.DamageAmount <= 0) return damageInfo.DamageAmount;

        if (CurrentShield <= 0 || IsBroken)
        {
            timer = settings.brokenRegenDelay;
            return damageInfo.DamageAmount;
        }

        float multiplier = 1f - (settings.damageReductionPercent / 100f);
        float modifiedDamage = damageInfo.DamageAmount * multiplier;

        if (modifiedDamage <= CurrentShield)
        {
            CurrentShield -= modifiedDamage;
            timer = settings.regenDelay;

            OnShieldDamaged?.Invoke(CurrentShield);
            return 0f;
        }
        else
        {
            // Convert absorbed shield damage back to its original amount before reduction.
            float shieldAbsorbedRaw = CurrentShield / multiplier;
            float leftoverDamage = damageInfo.DamageAmount - shieldAbsorbedRaw;

            CurrentShield = 50;
            IsBroken = true;
            timer = settings.brokenRegenDelay;

            OnShieldDamaged?.Invoke(CurrentShield);
            OnShieldBroken?.Invoke();

            return leftoverDamage;
        }
    }
}