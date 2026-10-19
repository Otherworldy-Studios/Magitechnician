using System;
using UnityEngine;

public class Healthbar : MonoBehaviour, IDamageable
{
    // Adopted teammate's float structure from main branch
    public float CurrentHealth { get; private set; }

    [field: SerializeField] public float MaxHealth { get; private set; } = 100;

    public event Action<float, GameObject> OnDamaged;
    public event Action<float, GameObject> OnHealed;
    public event Action<GameObject> OnDied;

    // YOUR SPRINT TASK: Invincibility tracking variables
    public bool IsInvincible { get; private set; }
    private float invincibilityTimer;

    // Teammate's tracking variables for Over-Time effects
    private float _damagePerSecond;
    private float _damageTimer = 0f;
    
    private float _healPerSecond;
    private float _healTimer = 0f;

    private void Awake()
    {
        CurrentHealth = MaxHealth;
    }

    private void Update()
    {
        // YOUR SPRINT TASK: Processes your invincibility countdown timer
        if (IsInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0)
            {
                IsInvincible = false;
            }
        }

        if (CurrentHealth <= 0f)
        {
            _healTimer = 0f;
            _damageTimer = 0f;
        }
        
        // Teammate's Healing over time
        if (_healTimer > 0f)
        {
            float delta = Mathf.Min(Time.deltaTime, _healTimer);
            float heal = _healPerSecond * delta;
            
            RegenHealth(heal);
            _healTimer -= delta;
        }
        
        // Teammate's Damaging over time
        if (_damageTimer > 0f)
        {
            float delta = Mathf.Min(Time.deltaTime, _damageTimer);
            float damage = _damagePerSecond * delta;
            
            TakeDamage(new DamageInfo(){DamageAmount = damage});
            _damageTimer -= delta;
        }
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        // YOUR SPRINT TASK: Block incoming damage if invincible
        if (IsInvincible) return;

        if (CurrentHealth <= 0 || damageInfo.DamageAmount <= 0) return;

        // Uses teammate's float damage system directly now
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
        if (CurrentHealth <= 0  || amount <= 0f) return;

        CurrentHealth += amount;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);

        OnHealed?.Invoke(CurrentHealth, gameObject);
    }

    // YOUR SPRINT TASK: Fulfills 'Add timed invincibility to Health' acceptance card
    public void TriggerInvincibility(float duration)
    {
        if (duration > 0)
        {
            IsInvincible = true;
            invincibilityTimer = duration;
        }
    }

    // Teammate's utility systems
    public void TakeDamageOverTime(DamageInfo damageInfo, float time)
    {
        if (CurrentHealth <= 0 || time <= 0f) return;

        _damagePerSecond = damageInfo.DamageAmount / time;
        _damageTimer = time;
    }

    public void RegenHealthOverTime(float amount, float time)
    {
        if (time <= 0f)
            return;
        
        _healPerSecond = amount / time;
        _healTimer = time;
    }
}

