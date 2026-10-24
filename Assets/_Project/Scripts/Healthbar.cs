using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

public class Healthbar : MonoBehaviour
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

    private readonly HashSet<Object> _invincibilityTokens = new();
    
    private bool _isInvincible => _invincibilityTokens.Count > 0;

    private void Awake()
    {
        CurrentHealth = MaxHealth;
    }

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
    
    public object StartInvincibility()
    {
        object token = new object();
        _invincibilityTokens.Add(token);
        
        return token;
    }

    public void StopInvincibility(object token)
    {
        if (token == null)
            return;
        
        _invincibilityTokens.Remove(token);
    }
    
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
