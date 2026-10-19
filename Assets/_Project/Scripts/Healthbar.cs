using System;
using UnityEngine;

public class Healthbar : MonoBehaviour, IDamageable
{
    // 1. Core tracking variables (Original logic)
    public int CurrentHealth { get; private set; }

    [field: SerializeField] public int MaxHealth { get; private set; } = 100;

    public event Action<int, GameObject> OnDamaged;
    public event Action<int, GameObject> OnHealed;
    public event Action<GameObject> OnDied;

    // 2. Sprint requirements: Variables used by Update and TriggerInvincibility
    public bool IsInvincible { get; private set; }
    private float invincibilityTimer;

    void Start()
    {
        CurrentHealth = MaxHealth;
    }

    // 3. Game frame loops
    void Update()
    {
        if (IsInvincible)
        {
            invincibilityTimer -= Time.deltaTime;

            if (invincibilityTimer <= 0)
            {
                IsInvincible = false;
            }
        }
    }

    // 4. Interface compliance method (Integrates teammate's float DamageAmount)
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (IsInvincible) return;

        if (CurrentHealth <= 0) return;

        CurrentHealth -= Mathf.RoundToInt(damageInfo.DamageAmount);

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);

        OnDamaged?.Invoke(CurrentHealth, gameObject);

        if (CurrentHealth <= 0)
        {
            OnDied?.Invoke(gameObject);
        }
    }

    // 5. Health utility functions
    public void RegenHealth(int amount)
    {
        if (CurrentHealth <= 0) return;

        CurrentHealth += amount;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);

        OnHealed?.Invoke(CurrentHealth, gameObject);
    }

    /// <summary>
    /// Fulfills the 'Add timed invincibility to Health' acceptance criteria card.
    /// </summary>
    public void TriggerInvincibility(float duration)
    {
        if (duration > 0)
        {
            IsInvincible = true;
            invincibilityTimer = duration;
        }
    }
}