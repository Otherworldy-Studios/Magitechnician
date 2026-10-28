using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

public class Health : MonoBehaviour
{
    [field: SerializeField] public float CurrentHealth { get; private set; }

    [field: SerializeField] public float MaxHealth { get; private set; } = 100;

    public event Action<float, GameObject> OnDamaged;
    public event Action<float, GameObject> OnHealed;
    public event Action<GameObject> OnDied;

    private float _damagePerSecond;
    private float _damageTimer = 0f;

    private float _healPerSecond;
    private float _healTimer = 0f;

    // Tokens allow multiple sources of invincibility to work independently.
    private readonly HashSet<Object> _invincibilityTokens = new();

    private bool _isInvincible => _invincibilityTokens.Count > 0;

    private void Awake()
    {
        CurrentHealth = MaxHealth;
    }

    /// <summary>
    /// Processes healing and damage over time, limiting the final tick
    /// to the remaining duration.
    /// </summary>
    private void Update()
    {
        if(CurrentHealth <= 0f)
        {
            _healTimer = 0f;
            _damageTimer = 0f;
        }

        //Healing over time
        if (_healTimer > 0f)
        {
            float delta = Mathf.Min(Time.deltaTime, _healTimer);
            float heal = _healPerSecond * delta;

            RegenHealth(heal);
            _healTimer -= delta;
        }

        //Damaging over time
        if (_damageTimer > 0f)
        {
            float delta = Mathf.Min(Time.deltaTime, _damageTimer);
            float damage = _damagePerSecond * delta;

            TakeDamage(new DamageInfo(){DamageAmount = damage});
            _damageTimer -= delta;
        }
    }

    /// <summary>
    /// Applies damage unless the target is dead or invincible,
    /// and triggers the appropriate health events.
    /// </summary>
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (CurrentHealth <= 0 || damageInfo.DamageAmount <= 0) return;

        if (_isInvincible)
            return; 

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

    /// <summary>
    /// Spreads the total damage evenly over the specified duration.
    /// Replaces any existing damage-over-time effect.
    /// </summary>
    public void TakeDamageOverTime(DamageInfo damageInfo, float time)
    {
        if (CurrentHealth <= 0 || time <= 0f) return;

        _damagePerSecond = damageInfo.DamageAmount / time;
        _damageTimer = time;
    }

    /// <summary>
    /// Spreads healing evenly over the specified duration.
    /// Replaces any existing healing-over-time effect.
    /// </summary>
    public void RegenHealthOverTime(float amount, float time)
    {
        if (time <= 0f)
            return;

        _healPerSecond = amount / time;
        _healTimer = time;
    }

    /// <summary>
    /// Creates a unique invincibility token that must be returned
    /// to remove this source of invincibility.
    /// </summary>
    public object StartInvincibility()
    {
        object token = new object();
        _invincibilityTokens.Add(token);

        return token;
    }

    /// <summary>
    /// Removes a specific invincibility token without affecting others.
    /// </summary>
    public void StopInvincibility(object token)
    {
        if (token == null)
            return;

        _invincibilityTokens.Remove(token);
    }

    /// <summary>
    /// Grants temporary invincibility using an independent token.
    /// </summary>
    public void MakeInvincible(float invincibleTimeSeconds)
    {
        StartCoroutine(InvincibilityTimer(invincibleTimeSeconds));
    }

    private IEnumerator InvincibilityTimer(float invincibleTimeSeconds)
    {
        object token = StartInvincibility();

        yield return new WaitForSeconds(invincibleTimeSeconds);

        StopInvincibility(token);
    }
}