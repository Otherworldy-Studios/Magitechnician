using System;
using System.Collections;
using UnityEngine;

public class Healthbar : MonoBehaviour, IDamageable
{
    private bool isInvincible = false;
    public float CurrentHealth { get; private set; }

    [field: SerializeField] public float MaxHealth { get; private set; } = 100;

    public event Action<float, GameObject> OnDamaged;
    public event Action<float, GameObject> OnHealed;
    public event Action<GameObject> OnDied;

    void Start()
    {
        CurrentHealth = MaxHealth;
    }
    
    public void SetInvincible(bool value) //Switch Tool
    {
        isInvincible = value;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (isInvincible)
        {
            return;
        }
        
        if (CurrentHealth <= 0) return;

        CurrentHealth -= damageInfo.DamageAmount;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);

        OnDamaged?.Invoke(CurrentHealth, gameObject);

        if (CurrentHealth <= 0)
        {
            OnDied?.Invoke(gameObject);
        }
    }
    
    public void MakeInvincible(float invincibleTimeSeconds)
    {
        StartCoroutine(InvincibilityTimer(invincibleTimeSeconds));
    }

    private IEnumerator InvincibilityTimer(float invincibleTimeSeconds)
    {
        isInvincible = true;
        
        //Invicible started
        
        yield return new WaitForSeconds(invincibleTimeSeconds);
        
        //Invicible ended
        
        isInvincible = false;
        
        
    }

    public void RegenHealth(float amount)
    {
        if (CurrentHealth <= 0) return;

        CurrentHealth += amount;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);

        OnHealed?.Invoke(CurrentHealth, gameObject);
    }
    
    
    
}
