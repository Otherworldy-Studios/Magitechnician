using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Holds one object's stats: reads the base values from a StatBlock, keeps copies of the modifiers added to it,
/// and works out each stat's final value. Scripts read values with GetValue and react to OnStatChanged.
/// The player and the gun each have their own Stats. Items add modifiers through PlayerRoot or Inventory,
/// which send each one to whichever Stats has its stat.
/// </summary>
public class Stats : MonoBehaviour
{
    [Tooltip("The stats this object has and their base values.")]
    [SerializeField] private StatBlock _statBlock;
    [Tooltip("The modifiers currently applied. Filled at run time.")]
    [SerializeField] private List<StatModifier> _modifiers;

    // Final value of every stat in the StatBlock, after modifiers
    [ShowInInspector, ReadOnly]
    private Dictionary<StatDefinition, float> _finalValues;

    /// <summary>Fires when a stat's final value changes, with the stat and its new value.</summary>
    public event Action<StatDefinition, float> OnStatChanged;

    /// <summary>Reads the StatBlock and works out the starting values. Runs before any other script reads a stat in Start.</summary>
    private void Awake()
    {
        InitializeStats();
    }

    /// <summary>Counts down timed modifiers every frame.</summary>
    private void Update()
    {
        RemoveExpiredModifiers();
    }

    /// <summary>Removes every modifier and returns each stat to its base value.</summary>
    [Button, DisableInEditorMode]
    public void ClearAllModifiers()
    {
        _modifiers.Clear();
        for (int i = 0; i < _statBlock.Entries.Count; i++)
        {
            Recalculate(_statBlock.Entries[i].Stat);
        }
    }

    /// <summary>
    /// Returns a stat's final value. Logs a warning and returns 0 for a stat this object's StatBlock doesn't list,
    /// so check HasStat first when a stat might be missing.
    /// </summary>
    /// <param name="stat">The stat to read.</param>
    public float GetValue(StatDefinition stat)
    {
        if (HasStat(stat))
        {
            return _finalValues[stat];
        }
        else
        {
            Debug.LogWarning($"Stat {stat.name} isn't assigned to this StatBlock. Please check the StatBlock assigned to {gameObject.name}.");
            return 0f;
        }
    }

    /// <summary>
    /// Returns what a stat's final value would be with one source's modifiers taken off and other modifiers added,
    /// without changing anything. The reward screen uses it to show how a gun part would change a stat.
    /// Returns 0 for a stat this object's StatBlock doesn't list.
    /// </summary>
    /// <param name="stat">The stat to preview.</param>
    /// <param name="removedSource">The source whose modifiers are left out, such as the part being replaced. Can be null.</param>
    /// <param name="addedModifiers">Modifiers to include as if they were added, such as the new part's. Can be null.</param>
    public float PreviewValue(StatDefinition stat, object removedSource, IReadOnlyList<StatModifier> addedModifiers)
    {
        if (!HasStat(stat)) return 0f;

        List<StatModifier> modifiers = new List<StatModifier>();

        foreach (StatModifier modifier in _modifiers)
        {
            if (removedSource == null || modifier.Source != removedSource)
                modifiers.Add(modifier);
        }

        if (addedModifiers != null)
        {
            foreach (StatModifier modifier in addedModifiers)
            {
                if (modifier != null) modifiers.Add(modifier);
            }
        }

        return CalculateValue(stat, modifiers);
    }

    /// <summary>Returns the list of modifiers currently applied.</summary>
    public List<StatModifier> GetModifiers()
    {
        return _modifiers;
    }

    /// <summary>
    /// True when this object's StatBlock lists the stat. PlayerRoot and Inventory use it to send each modifier
    /// to the player's Stats or the gun's. Every script's ReadStat helper uses it to fall back to a default.
    /// </summary>
    /// <param name="stat">The stat to look for.</param>
    public bool HasStat(StatDefinition stat)
    {
        return stat != null &&
            _finalValues != null &&
            _finalValues.ContainsKey(stat);
    }

    /// <summary>
    /// Adds a copy of a modifier, with the object that added it as its source, and recalculates its stat.
    /// The item asset's own modifier is never stored or changed. Returns the stored copy, or null if the modifier
    /// is empty or its stat isn't in this StatBlock.
    /// </summary>
    /// <param name="modifier">The modifier from an item asset.</param>
    /// <param name="source">The item adding it. RemoveAllFromSource(source) removes it later. Optional, but without it nothing can remove the modifier by source.</param>
    public StatModifier AddModifier(StatModifier modifier, object source = null)
    {
        if (modifier == null || modifier.Stat == null)
        {
            Debug.LogWarning($"Tried to add an empty modifier to {gameObject.name}.");
            return null;
        }

        if (!HasStat(modifier.Stat))
        {
            Debug.LogWarning($"{modifier.Stat.name} isn't in the StatBlock on {gameObject.name}. Modifier not added.");
            return null;
        }

        StatModifier copy = modifier.CopyWithSource(source);

        _modifiers.Add(copy);
        Recalculate(copy.Stat);
        return copy;
    }

    /// <summary>Removes one stored modifier and recalculates its stat. Pass the copy AddModifier returned.</summary>
    /// <param name="modifier">The stored copy to remove.</param>
    public void RemoveModifier(StatModifier modifier)
    {
        if (!_modifiers.Contains(modifier))
        {
            Debug.LogWarning($"Modifier {modifier} not found in the list of modifiers for {gameObject.name}. Cannot remove.");
        }
        else
        {
            _modifiers.Remove(modifier);
            Recalculate(modifier.Stat);
        }
    }

    /// <summary>
    /// Removes every modifier from one source and recalculates each affected stat. PlayerRoot.RemoveModifiers and
    /// Inventory call this when an item comes off.
    /// </summary>
    /// <param name="source">The item or object that added the modifiers.</param>
    public void RemoveAllFromSource(object source)
    {
        if (source == null) return;

        for (int i = _modifiers.Count - 1; i >= 0; i--)
        {
            StatModifier modifier = _modifiers[i];
            if (modifier.Source != source) continue;

            _modifiers.RemoveAt(i);
            Recalculate(modifier.Stat);
        }
    }

    /// <summary>Counts down every timed modifier and removes the ones that ran out.</summary>
    private void RemoveExpiredModifiers()
    {
        for (int i = _modifiers.Count - 1; i >= 0; i--)
        {
            StatModifier modifier = _modifiers[i];
            if (!modifier.Tick(Time.deltaTime)) continue;

            _modifiers.RemoveAt(i);
            Recalculate(modifier.Stat);
        }
    }

    /// <summary>Stores one stat's final value from CalculateValue. Fires OnStatChanged only if the value changed.</summary>
    /// <param name="stat">The stat to recalculate.</param>
    private void Recalculate(StatDefinition stat)
    {
        if (!HasStat(stat)) return;

        float finalValue = CalculateValue(stat, _modifiers);

        _finalValues.TryGetValue(stat, out float oldValue);
        _finalValues[stat] = finalValue;

        if (!Mathf.Approximately(oldValue, finalValue))
        {
            OnStatChanged?.Invoke(stat, finalValue);
        }
    }

    /// <summary>
    /// Works out one stat's final value from a list of modifiers: (base + every Add) × every Multiply, or the latest
    /// Set if there is one, then clamps it to the stat's range.
    /// </summary>
    private float CalculateValue(StatDefinition stat, IReadOnlyList<StatModifier> modifiers)
    {
        float baseValue = 0f;
        float additiveValue = 0f;
        float multiplicativeValue = 1f;
        bool hasSet = false;
        float setValue = 0f;

        foreach (StatEntry entry in _statBlock.Entries)
        {
            if (entry.Stat == stat)
            {
                baseValue = entry.BaseValue;
            }
        }

        foreach (StatModifier modifier in modifiers)
        {
            if (modifier.Stat != stat)
            {
                continue;
            }

            switch (modifier.Operation)
            {
                case ModifierOperation.Add:
                    additiveValue += modifier.Value;
                    break;
                case ModifierOperation.Multiply:
                    multiplicativeValue *= modifier.Value;
                    break;
                case ModifierOperation.Set:
                    // The most recently added Set wins.
                    hasSet = true;
                    setValue = modifier.Value;
                    break;
            }
        }

        float finalValue = hasSet ? setValue : (baseValue + additiveValue) * multiplicativeValue;
        return stat.Clamp(finalValue);
    }

    /// <summary>Fills the final values from the StatBlock's base values and starts with no modifiers.</summary>
    private void InitializeStats()
    {
        _finalValues = new Dictionary<StatDefinition, float>();
        _modifiers = new List<StatModifier>();
        for (int i = 0; i < _statBlock.Entries.Count; i++)
        {
            _finalValues[_statBlock.Entries[i].Stat] = _statBlock.Entries[i].BaseValue;
            Recalculate(_statBlock.Entries[i].Stat);
        }
    }
}