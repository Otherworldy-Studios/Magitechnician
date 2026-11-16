using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// An entity's health: damage, healing, healing over time, and invincibility, with events for each change.
/// DamageReceiver calls TakeDamage after the shield takes its share. Damage over time lives in DamageReceiver.
/// Max health comes from the MaxHealth stat when Stats and the stat are assigned.
/// </summary>
public class Health : MonoBehaviour
{
    /// <summary>Current health, from 0 to MaxHealth.</summary>
    [field: SerializeField] public int CurrentHealth { get; private set; }

    /// <summary>The most health this entity can have. Set from Stat_MaxHealth in Start, or the Inspector value without stats.</summary>
    [field: SerializeField] public int MaxHealth { get; private set; } = 100;

    /// <summary>Fires after damage, with the new current health and this object.</summary>
    public event Action<int, GameObject> OnDamaged;
    /// <summary>Fires after healing, with the new current health and this object.</summary>
    public event Action<int, GameObject> OnHealed;
    /// <summary>Fires once when health reaches 0, with this object. GameManager ends the run on the player's; EnemyBase dies on an enemy's.</summary>
    public event Action<GameObject> OnDied;

    // Healing over time. RegenHealthOverTime sets these, and Update spreads the total across the duration.
    private float _healAccumulator;   // Fractional healing not applied yet, because health is whole numbers.
    private float _healDuration;      // Total seconds the current heal lasts.
    private int _healOverTimeAmount;  // Total health the current heal gives.
    private float _healTimer = 0f;    // Seconds left on the current heal.

    // Each source of invincibility holds its own token, such as the dash and the debug button,
    // so one source ending doesn't cancel another. The entity is invincible while any token is held.
    private readonly HashSet<object> _invincibilityTokens = new();

    private object _debugInvincibilityToken; // INTEGRATION: new. The token held by the Toggle Invincibility debug button.

    private bool IsInvincible => _invincibilityTokens.Count > 0;

    // INTEGRATION: MaxHealth reads this stat. PlayerRoot.AssignStats (on the player) or EnemyBase.Awake (on an enemy)
    // hands this component its Stats through SetStats.
    [Tooltip("The Stats to read MaxHealth from. PlayerRoot or EnemyBase fills this in Awake if it's empty.")]
    [SerializeField] private Stats _stats;
    [Tooltip("Stat_MaxHealth. Without it, the Max Health value above is used.")]
    [SerializeField] private StatDefinition _maxHealthStat;

    /// <summary>Gives this component the Stats to read from. Keeps a Stats already assigned in the Inspector.</summary>
    // INTEGRATION: new. Called by PlayerRoot.AssignStats and EnemyBase.Awake.
    public void SetStats(Stats playerStats)
    {
        if (_stats == null)
        {
            _stats = playerStats;
        }
    }

    /// <summary>
    /// Sets MaxHealth from the stat, starts listening for changes to it, and fills health.
    /// Runs in Start, after every Awake, so Stats has its values and SetStats has run.
    /// </summary>
    // INTEGRATION: was Awake, which filled health from the serialized MaxHealth. EnemyBase used to check for 0 health in its
    // Start, which could run before this one; that check was removed, and death only comes through OnDied.
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
    /// Applies healing over time, spreading the total evenly and limiting the last tick to the time left.
    /// Stops healing once the entity is dead.
    /// </summary>
    private void Update()
    {
        if (CurrentHealth <= 0f)
        {
            _healTimer = 0f;
        }

        if (_healTimer > 0f)
        {
            float delta = Mathf.Min(Time.deltaTime, _healTimer);
            _healAccumulator += (_healOverTimeAmount / _healDuration) * delta;

            // Only whole points are applied. The rest waits in the accumulator.
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
    /// Lowers health unless the entity is dead or invincible, then fires OnDamaged, and OnDied at 0.
    /// </summary>
    /// <param name="damageInfo">The hit, with the amount left after the shield.</param>
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (CurrentHealth <= 0 || damageInfo.DamageAmount <= 0) return;

        if (IsInvincible)
            return;

        CurrentHealth -= damageInfo.DamageAmount;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);

        OnDamaged?.Invoke(CurrentHealth, gameObject);

        if (CurrentHealth <= 0)
        {
            OnDied?.Invoke(gameObject);
        }
    }

    /// <summary>Restores health up to MaxHealth and fires OnHealed. Does nothing at 0 health, so it can't revive.</summary>
    /// <param name="amount">Health to restore. Inventory.UseConsumable passes a consumable's InstantHeal.</param>
    public void RegenHealth(int amount)
    {
        if (CurrentHealth <= 0 || amount <= 0f) return;

        CurrentHealth += amount;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);

        OnHealed?.Invoke(CurrentHealth, gameObject);
    }

    /// <summary>
    /// Spreads healing evenly over a duration. Replaces any healing over time already running.
    /// </summary>
    /// <param name="amount">Total health to restore.</param>
    /// <param name="time">Seconds to spread it over.</param>
    public void RegenHealthOverTime(int amount, float time)
    {
        if (time <= 0f)
            return;

        _healTimer = time;
        _healDuration = time;
        _healOverTimeAmount = amount;
    }

    /// <summary>
    /// Makes the entity invincible until the returned token is passed to StopInvincibility.
    /// Dash uses this for its invincibility frames.
    /// </summary>
    public object StartInvincibility()
    {
        object token = new object();
        _invincibilityTokens.Add(token);

        return token;
    }

    /// <summary>Ends one source of invincibility without affecting others.</summary>
    /// <param name="token">The token StartInvincibility returned.</param>
    public void StopInvincibility(object token)
    {
        if (token == null)
            return;

        _invincibilityTokens.Remove(token);
    }

    /// <summary>Makes the entity invincible for a number of seconds, with its own token.</summary>
    public void MakeInvincible(float invincibleTimeSeconds)
    {
        StartCoroutine(InvincibilityTimer(invincibleTimeSeconds));
    }

    /// <summary>Holds a token for the given time, then returns it.</summary>
    private IEnumerator InvincibilityTimer(float invincibleTimeSeconds)
    {
        object token = StartInvincibility();

        yield return new WaitForSeconds(invincibleTimeSeconds);

        StopInvincibility(token);
    }

    /// <summary>
    /// Follows changes to Stat_MaxHealth, such as from a charm, stock, or consumable. Gaining max health adds the
    /// difference to current health. Losing it only lowers current health if it's now over the max.
    /// </summary>
    // INTEGRATION: new. Listens to Stats.OnStatChanged, which fires when PlayerRoot or Inventory adds or removes a modifier.
    private void HandleStatChanged(StatDefinition stat, float value)
    {
        if (stat != _maxHealthStat) return;

        int newMax = Mathf.RoundToInt(value);
        int difference = newMax - MaxHealth;
        MaxHealth = newMax;

        if (difference > 0) CurrentHealth += difference;
        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
    }

    #region Debug
    // INTEGRATION: HealthDebug's F1, F4, and F5 keys became these Inspector buttons, and HealthDebug.cs was deleted.

    /// <summary>Debug: heals by an amount.</summary>
    [Button, DisableInEditorMode]
    private void DebugHeal(int amount = 25)
    {
        RegenHealth(amount);
    }

    /// <summary>Debug: heals an amount over a number of seconds.</summary>
    [Button, DisableInEditorMode]
    private void DebugHealOverTime(int amount = 50, float seconds = 10f)
    {
        RegenHealthOverTime(amount, seconds);
    }

    /// <summary>Debug: stops healing over time.</summary>
    [Button, DisableInEditorMode]
    private void DebugStopHealOverTime()
    {
        _healTimer = 0f;
    }

    /// <summary>Debug: turns invincibility on or off with its own token, so it doesn't interfere with the dash's.</summary>
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
