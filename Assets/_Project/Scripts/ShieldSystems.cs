using System;
using UnityEngine;

public class ShieldSystem : MonoBehaviour, IDamageable
{
    [SerializeField] private ShieldSettings settings;

    public float CurrentShield { get; private set; }
    public bool IsBroken { get; private set; }

    public event Action<float, GameObject> OnShieldDamaged;
    public event Action<GameObject> OnShieldBroken;
    public event Action<GameObject> OnShieldRestored;

    private float timer;

    void Start()
    {
        if (settings != null)
        {
            CurrentShield = settings.maxShield;
        }
    }

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
                OnShieldRestored?.Invoke(gameObject);
            }
        }
    }

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

            OnShieldDamaged?.Invoke(CurrentShield, gameObject);
            return 0f;
        }
        else
        {
            float shieldAbsorbedRaw = CurrentShield / multiplier;
            float leftoverDamage = damageInfo.DamageAmount - shieldAbsorbedRaw;

            CurrentShield = 0;
            IsBroken = true;
            timer = settings.brokenRegenDelay;

            OnShieldDamaged?.Invoke(CurrentShield, gameObject);
            OnShieldBroken?.Invoke(gameObject);

            return leftoverDamage;
        }
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        AbsorbDamageAndReturnOverflow(damageInfo);
    }
}
