using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

public class Health : MonoBehaviour
{
    [field: SerializeField] public int CurrentHealth { get; private set; }

    [field: SerializeField] public int MaxHealth { get; private set; } = 100;

    public event Action<int, GameObject> OnDamaged;
    public event Action<int, GameObject> OnHealed;
    public event Action<GameObject> OnDied;

    private float _healAccumulator;
    private float _healDuration;
    private int _healOverTimeAmount;
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
        }

        //Healing over time
        if (_healTimer > 0f)
        {
            float delta = Mathf.Min(Time.deltaTime, _healTimer);
            _healAccumulator += (_healOverTimeAmount / _healDuration) * delta;

            int heal = Mathf.FloorToInt(_healAccumulator);
            if (heal > 0)
            {
                RegenHealth(heal);
                _healAccumulator -= heal;
            }
            
            _healTimer -= delta;
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

    public void RegenHealth(int amount)
    {
        if (CurrentHealth <= 0  || amount <= 0f) return;

        CurrentHealth += amount;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);

        OnHealed?.Invoke(CurrentHealth, gameObject);
    }

    /// <summary>
    /// Spreads healing evenly over the specified duration.
    /// Replaces any existing healing-over-time effect.
    /// </summary>
    public void RegenHealthOverTime(int amount, float time)
    {
        if (time <= 0f)
            return;
        
        _healTimer = time;
        _healDuration = time;
        _healOverTimeAmount = amount;
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