using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


// Debugging tool for Stats
// Debug keys:
//   1 = toggle speed x1.5        (permanent, from the debug charm)
//   2 = toggle maxHealth +50     (permanent, from the debug charm)
//   3 = toggle baseDamage +5     (permanent, from the debug charm)
//   4 = use the debug consumable: temporary buff to all three stats
//   5 = unequip the debug charm: removes only the charm's modifiers
//   0 = remove every debug modifier

[RequireComponent(typeof(Stats))]
public class StatsDebug : MonoBehaviour
{
    //What happens when the consumable is used while the previous one is still active:
    // Stacking: each use creates a new modifier with its own duration, so they can stack.
    // Refresh: each use refreshes the duration of the existing modifier, so they don't stack.
    // Ignore: each use is ignored if the existing modifier is still active, so they don't stack.
    // For now, we are using Stacking, but this could be changed in the future.
    public enum ConsumableBehavior
    {
        Stacking,
        Refresh,
        Ignore
    }

    // Read every time the consumable is used, so it can be tweaked in the Inspector during Play mode.
    [SerializeField] private float _consumableDuration = 5f;
    [SerializeField] private ConsumableBehavior _consumableBehavior = ConsumableBehavior.Stacking;

    private Stats _stats;

    // Stand-in for the real charm and consumable items until the item system exists.
    // So charms and consumables can be the Source of modifiers, so that unequipping them removes only their modifiers.
    private Charms _debugCharm;
    private Consumables _debugConsumable;

    // Remember which modifiers have been added from the debug charm, so they can be removed when the charm is unequipped.
    // RemoveModifier works by reference.
    private readonly Dictionary<statType, EntityStatsModifier> _testModifiers = new Dictionary<statType, EntityStatsModifier>();

    // Modifiers added by the consumable that may still be active. Used to tell whether the buff is running.
    private readonly List<EntityStatsModifier> _consumableModifiers = new List<EntityStatsModifier>();

    private void Awake()
    {
        _stats = GetComponent<Stats>();
        _debugCharm = new Charms("Debug Charm");
        _debugConsumable = new Consumables("Debug Consumable");
    }


    private void OnEnable()
    {
        _stats.OnStatChanged += LogChange;
    }

    private void OnDisable()
    {
        _stats.OnStatChanged -= LogChange;
    }

    private void Update()
    {
        if (Pressed(1))
            Toggle(statType.speed, modifierType.multiply, 1.5f);
        if (Pressed(2))
            Toggle(statType.maxHealth, modifierType.add, 50f);
        if (Pressed(3)) 
            Toggle(statType.baseDamage, modifierType.add, 5f);

        if (Pressed(4))
            UseConsumable();

        if (Pressed(5))
        {
            _stats.RemoveAllFromSource(_debugCharm);
            _testModifiers.Clear();
            Debug.Log($"[StatsDebug] Unequipped {_debugCharm}", this);
        }

        if (Pressed(0))
        {
            _stats.RemoveAllFromSource(_debugCharm);
            _stats.RemoveAllFromSource(_debugConsumable);
            _testModifiers.Clear();
            _consumableModifiers.Clear();
            Debug.Log("[StatsDebug] Removed all debug modifiers", this);
        }
    }
    private void Toggle(statType stat, modifierType type, float value)
    {
        if (_testModifiers.TryGetValue(stat, out EntityStatsModifier existingModifier))
        {
            _stats.RemoveModifier(existingModifier);
            _testModifiers.Remove(stat);
            Debug.Log($"[StatsDebug] Removed {existingModifier}", this);
        }
        else
        {
            var modifier = new EntityStatsModifier(stat, type, value, _debugCharm);
            _stats.AddModifier(modifier);
            _testModifiers[stat] = modifier;
            Debug.Log($"[StatsDebug] Added {modifier}", this);
        }
    }

    private void UseConsumable()
    {
        // A duration of 0 would make the modifiers permanent, which a consumable should never be.
        float duration = _consumableDuration;
        if (duration <= 0f)
        {
            Debug.LogWarning($"[StatsDebug] Consumable duration must be greater than 0 (currently {duration:0.00}).", this);
            return;
        }

        // Stats removes expired modifiers on its own; drop them here too so the list only holds active ones.
        _consumableModifiers.RemoveAll(m => m.isExpired);
        bool isActive = _consumableModifiers.Count > 0;

        if (isActive && _consumableBehavior == ConsumableBehavior.Ignore)
        {
            Debug.Log($"[StatsDebug] {_debugConsumable} is already active; ignored.", this);
            return;
        }

        if (isActive && _consumableBehavior == ConsumableBehavior.Refresh)
        {
            _stats.RemoveAllFromSource(_debugConsumable);
            _consumableModifiers.Clear();
        }

        // New modifier instances every use, each with its own countdown.
        AddConsumableModifier(statType.speed, modifierType.multiply, 1.5f, duration);
        AddConsumableModifier(statType.maxHealth, modifierType.add, 50f, duration);
        AddConsumableModifier(statType.baseDamage, modifierType.add, 5f, duration);

        string action = isActive ? (_consumableBehavior == ConsumableBehavior.Refresh ? "Refreshed" : "Stacked") : "Used";
        Debug.Log($"[StatsDebug] {action} {_debugConsumable} for {duration:0.00}s", this);
    }

    private void AddConsumableModifier(statType stat, modifierType type, float value, float duration)
    {
        var modifier = new EntityStatsModifier(stat, type, value, _debugConsumable, duration);
        _stats.AddModifier(modifier);
        _consumableModifiers.Add(modifier);
        Debug.Log($"[StatsDebug] Added {modifier}", this);
    }

    private void LogChange(statType stat, float newValue)
    {
        Debug.Log($"[StatsDebug] {name} {stat} -> {newValue:0.00} (base: {_stats.GetBaseValue(stat):0.00})", this);
    }

    private bool Pressed(int key)
    {
        Keyboard kb = Keyboard.current;
        if (kb == null)
            return false;

        return key switch
        {
            0 => kb.digit0Key.wasPressedThisFrame,
            1 => kb.digit1Key.wasPressedThisFrame,
            2 => kb.digit2Key.wasPressedThisFrame,
            3 => kb.digit3Key.wasPressedThisFrame,
            4 => kb.digit4Key.wasPressedThisFrame,
            5 => kb.digit5Key.wasPressedThisFrame,
            _ => false
        };
    }
}

