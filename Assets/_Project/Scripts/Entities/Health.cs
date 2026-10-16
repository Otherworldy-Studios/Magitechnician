using System;
using UnityEngine;

//Health class holds current health and handles damage, death and healing. Also listens for maxHealth changes from Stats.
[RequireComponent(typeof(Stats))]
public class Health : MonoBehaviour
{
    //Current, max
    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;
    public event Action OnRevive;

    public float currentHealth { get; private set; }
    public float maxHealth { get; private set; }
    
    //Set once when death happens, so OnDeath is only called once per Death.
    public bool isDead { get; private set; }

    private Stats _stats;

    private void Awake()
    {
        _stats = GetComponent<Stats>();
        maxHealth = _stats.maxHealth;
        currentHealth = maxHealth;
    }

    private void OnEnable()
    {
        _stats.OnStatChanged += HandleStatChanged;
        SyncMaxHealth(_stats.maxHealth); //Sync max health on enable in case it changed while disabled
    }

    private void OnDisable()
    {
        _stats.OnStatChanged -= HandleStatChanged;
    }

    private void Start() 
    {
        //Provides listeners with the initial health values when the game starts.
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        CheckForDeath(); //Check if the entity is dead at the start of the game.
    }


    private void HandleStatChanged(statType stat, float newValue)
    {
        if (stat == statType.maxHealth)
        {
            SyncMaxHealth(newValue);
        }
    }


    //When max healts chan
    private void SyncMaxHealth(float newMax) 
    {
        float oldMax = maxHealth;
        float oldCurrent = currentHealth;
        maxHealth = newMax;
        if (!isDead)
        {
            if (oldMax <= 0f || oldCurrent >= oldMax)
            {
                //Was at full health (or max was 0): stay full. Avoids float drift at full health.
                currentHealth = maxHealth;
            }
            else
            {
                currentHealth = oldCurrent / oldMax * maxHealth;
            }
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }

        if (!Mathf.Approximately(oldMax, maxHealth) || !Mathf.Approximately(oldCurrent, currentHealth))
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        CheckForDeath(); //If max health is reduced to 0, the entity should die.
    }

    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0) 
            return;
        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        CheckForDeath();
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0) 
            return;
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    //In case player is dead and we want to revive them, we can use this method to reset health.
    //Does nothing if the entity is not dead.
    public void Revive()
    {
        if (!isDead || maxHealth <= 0)
            return;
        isDead = false;
        currentHealth = maxHealth;
        OnRevive?.Invoke();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void CheckForDeath()
    {
        if (isDead || currentHealth > 0)
        return;
        isDead = true;
        OnDeath?.Invoke();
    }
}
