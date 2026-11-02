using UnityEngine;
using System.Collections.Generic;
using System;

// This class is a MonoBehaviour that manages a collection of stats and their modifiers, allowing for the calculation of final 
// stat values based on base values and applied modifiers.
public class Stats : MonoBehaviour
{
    [SerializeField] private StatBlock _statBlock;
    [SerializeField] private List<StatModifier> _modifiers;
    [SerializeField] private Dictionary<StatDefinition, float> _finalValues;
    public event Action<StatDefinition, float> OnStatChanged;

    // This method is called when the script instance is being loaded. It initializes the final values of the stats based on the
    // base values defined in the StatBlock.
    public float GetValue(StatDefinition stat)
    {
        if (_finalValues.ContainsKey(stat))
        {
            return _finalValues[stat];
        }
        else
        {
            return 0f;
        }
    }

    // This method checks if a specific stat is present in the final values dictionary.
    public bool HasStat(StatDefinition stat)
    {
        return _finalValues.ContainsKey(stat);
    }

    // This method is called every frame. It updates the duration of each modifier and removes any that have expired.
    public void AddModifier(StatModifier modifier)
    {
        _modifiers.Add(modifier);
        Recalculate(modifier.Stat);
    }

    // This method removes a specific modifier from the list and recalculates the affected stat.
    public void RemoveModifier(StatModifier modifier)
    {
        _modifiers.Remove(modifier);
        Recalculate(modifier.Stat);
    }

    // This method removes all modifiers that originate from a specific source and recalculates the affected stats.
    public void RemoveAllFromSource(object source)
    {
        List<StatModifier> modifiersToRemove = new List<StatModifier>();
        for (int i = 0; i < _modifiers.Count; i++)
        {
            if (_modifiers[i].Source == source)
            {
                modifiersToRemove.Add(_modifiers[i]);
            }
        }

        for (int i = 0; i < modifiersToRemove.Count; i++)
        {
            _modifiers.Remove(modifiersToRemove[i]);
            Recalculate(modifiersToRemove[i].Stat);
        }
    }

    // This method updates the duration of each modifier based on the time elapsed since the last frame and removes any that have 
    // expired.
    private void RemoveExpiredModifiers()
    {
        List<StatModifier> expiredModifiers = new List<StatModifier>();
        for (int i = 0; i < _modifiers.Count; i++)
        {
            if (_modifiers[i].Duration <= 0f)
            {
                expiredModifiers.Add(_modifiers[i]);
            }
        }

        for (int i = 0; i < expiredModifiers.Count; i++)
        {
            _modifiers.Remove(expiredModifiers[i]);
            Recalculate(expiredModifiers[i].Stat);
        }
    }

    // This method recalculates the final value of a specific stat based on its base value and any applied modifiers. It takes 
    // into account both additive and multiplicative modifiers and clamps the final value within the defined min/max range for 
    // the stat.
    private void Recalculate(StatDefinition stat)
    {
        float baseValue = 0f;
        float additiveValue = 0f;
        float multiplicativeValue = 1f;

        for (int i = 0; i < _statBlock.Entries.Count; i++)
        {
            if (_statBlock.Entries[i].Stat == stat)
            {
                baseValue = _statBlock.Entries[i].BaseValue;
            }
        }

        for (int i = 0; i < _modifiers.Count; i++)
        {
            if (_modifiers[i].Stat == stat)
            {
                if (_modifiers[i].Operation == ModifierOperation.Add)
                {
                    additiveValue += _modifiers[i].Value;
                }
                else
                {
                    multiplicativeValue *= _modifiers[i].Value;
                }

                float finalValue = (baseValue + additiveValue) * multiplicativeValue;
                finalValue = stat.Clamp(finalValue);
                _finalValues[stat] = finalValue;
            }
        }
    }
}
