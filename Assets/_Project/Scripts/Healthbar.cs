using System;
using UnityEngine;

public class Healthbar : MonoBehaviour, IDamageable
{
    public int CurrentHealth { get; private set; }

    [field: SerializeField] public int MaxHealth { get; private set; } = 100;

    public event Action<int, GameObject> OnDamaged;
    public event Action<int, GameObject> OnHealed;
    public event Action<GameObject> OnDied;

    void Start()
    {
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (CurrentHealth <= 0) return;

        CurrentHealth -= damageInfo.amount;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);

        OnDamaged?.Invoke(CurrentHealth, gameObject);

        if (CurrentHealth <= 0)
        {
            OnDied?.Invoke(gameObject);
        }
    }

    public void RegenHealth(int amount)
    {
        if (CurrentHealth <= 0) return;

        CurrentHealth += amount;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);

        OnHealed?.Invoke(CurrentHealth, gameObject);
    }
}
