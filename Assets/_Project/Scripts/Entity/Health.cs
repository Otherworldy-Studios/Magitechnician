using Sirenix.OdinInspector;
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

    private object _debugInvincibilityToken;

    private bool _isInvincible => _invincibilityTokens.Count > 0;

    [SerializeField] private Stats _stats;
    [SerializeField] private StatDefinition _maxHealthStat;

    public void SetStats(Stats playerStats)
    {
        if (_stats == null)
        {
            _stats = playerStats;
        }
    }

    private void Start()
    {
        if (_stats != null && _maxHealthStat != null)
        {
            MaxHealth = Mathf.RoundToInt(_stats.GetValue(_maxHealthStat));
            _stats.OnStatChanged += HandleStatChanged;
        }

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

    // Runs when a charm, stock, or consumable changes max health.
    private void HandleStatChanged(StatDefinition stat, float value)
    {
        if (stat != _maxHealthStat) return;

        int newMax = Mathf.RoundToInt(value);
        int difference = newMax - MaxHealth;
        MaxHealth = newMax;

        // Gaining max health also adds it to current health. Losing it only lowers current health if it's now over the max.
        if (difference > 0) CurrentHealth += difference;
        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
    }

    #region Debug
    [Button, DisableInEditorMode]
    private void DebugHeal(int amount = 25)
    {
        RegenHealth(amount);
    }

    [Button, DisableInEditorMode]
    private void DebugHealOverTime(int amount = 50, float seconds = 10f)
    {
        RegenHealthOverTime(amount, seconds);
    }

    [Button, DisableInEditorMode]
    private void DebugStopHealOverTime()
    {
        _healTimer = 0f;
    }

    [Button, DisableInEditorMode]
    private void DebugToggleInvincibility()
    {
        if (_debugInvincibilityToken == null)
        {
            _debugInvincibilityToken = StartInvincibility();
        }
        else
        {
            StopInvincibility(_debugInvincibilityToken);
            _debugInvincibilityToken = null;
        }
    }
    #endregion
}