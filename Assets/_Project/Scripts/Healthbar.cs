using System;
using UnityEngine;

public class Healthbar : MonoBehaviour, IDamageable
{
    public float CurrentHealth { get; private set; }

    [field: SerializeField] public float MaxHealth { get; private set; } = 100;

    public event Action<float, GameObject> OnDamaged;
    public event Action<float, GameObject> OnHealed;
    public event Action<GameObject> OnDied;

    void Start()
    {
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (CurrentHealth <= 0) return;

        CurrentHealth -= damageInfo.DamageAmount;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);

        OnDamaged?.Invoke(CurrentHealth, gameObject);

        if (CurrentHealth <= 0)
        {
            OnDied?.Invoke(gameObject);
        }
    }

    public void RegenHealth(float amount)
    {
        if (CurrentHealth <= 0) return;

        CurrentHealth += amount;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);

        OnHealed?.Invoke(CurrentHealth, gameObject);
    }
}
