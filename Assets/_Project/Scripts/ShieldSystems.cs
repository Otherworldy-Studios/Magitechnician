using System;
using UnityEngine;

public class ShieldSystem : MonoBehaviour, IDamageable
{
    [SerializeField] private ShieldSettings settings;
    [SerializeField] private Healthbar healthScript;

    public float CurrentShield { get; private set; }
    public bool IsBroken { get; private set; }

    public event Action<float, GameObject> OnShieldDamaged;
    public event Action<GameObject> OnShieldBroken;

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
            CurrentShield += settings.regenRate * Time.deltaTime;
            CurrentShield = Mathf.Clamp(CurrentShield, 0, settings.maxShield);

            if (CurrentShield > 0)
            {
                IsBroken = false;
            }
        }
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (settings == null || damageInfo.DamageAmount <= 0) return;

        // If shield is already down, pass all incoming raw damage straight to health bar
        if (CurrentShield <= 0 || IsBroken)
        {
            if (healthScript != null)
            {
                healthScript.TakeDamage(damageInfo);
            }
            return;
        }

        // Percentage criteria: Reduce incoming hit amount by the reduction multiplier
        float multiplier = 1f - (settings.damageReductionPercent / 100f);
        float modifiedDamage = damageInfo.DamageAmount * multiplier;

        if (modifiedDamage <= CurrentShield)
        {
            // Shield has enough armor to absorb the hit entirely
            CurrentShield -= modifiedDamage;
            timer = settings.regenDelay;

            OnShieldDamaged?.Invoke(CurrentShield, gameObject);
        }
        else
        {
            // Overflow criteria: Calculate raw damage amount absorbed before breaking
            float shieldAbsorbedRaw = CurrentShield / multiplier;
            float leftoverDamage = damageInfo.DamageAmount - shieldAbsorbedRaw;

            CurrentShield = 0;
            IsBroken = true;
            timer = settings.brokenRegenDelay;

            OnShieldDamaged?.Invoke(CurrentShield, gameObject);
            OnShieldBroken?.Invoke(gameObject);

            if (healthScript != null)
            {
                damageInfo.DamageAmount = leftoverDamage;
                healthScript.TakeDamage(damageInfo);
            }
        }
    }
}
