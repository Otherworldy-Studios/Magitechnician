using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.InputSystem;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

// This class is a MonoBehaviour that manages a collection of stats and their modifiers, allowing for the calculation of final 
// stat values based on base values and applied modifiers.
public class Stats : MonoBehaviour
{
    [SerializeField] private StatBlock _statBlock;
    [SerializeField] private List<StatModifier> _modifiers;
    private Dictionary<StatDefinition, float> _finalValues;
    public event Action<StatDefinition, float> OnStatChanged;
    public StatDefinition testStat;

    public StatModifier TestModifier;
    public bool debug = true;


    // This method is called when the script instance is being loaded. It initializes the stats by calling the InitializeStats method.
    private void Awake()
    {
        InitializeStats();
    }

    private void Update()
    {
        DebugMod();
        RemoveExpiredModifiers();
    }

    public void DebugMod()
    {
#if UNITY_EDITOR
        if (Keyboard.current.mKey.wasPressedThisFrame && debug == true)
        {
            Debug.Log($"Adding modifier {TestModifier} to {gameObject.name}.");
            AddModifier(TestModifier);

        }
        if (Keyboard.current.rKey.wasPressedThisFrame && debug == true)
        {
            RemoveModifier(TestModifier);
        }
        if (Keyboard.current.nKey.wasPressedThisFrame && debug == true)
        {
            GetValue(testStat);
        }
        if (Keyboard.current.tKey.wasPressedThisFrame && debug == true)
        {
            foreach (StatDefinition stat in _finalValues.Keys)
            {
                Debug.Log($"Stat {stat.name} has a final value of {_finalValues[stat]}.");
            } 
        }
#endif
    }

    // This method retrieves the final value of a specific stat. If the stat is not present in the final values dictionary, it logs an error and returns 0.
    public float GetValue(StatDefinition stat)
    {
        if (HasStat(stat))
        {
            Debug.Log($"Stat {stat.name} has a final value of {_finalValues[stat]}.");
            return _finalValues[stat];
        }
        else
        {
            Debug.LogWarning($"Stat {stat.name} isn't assigned to this StatBlock. Please check the StatBlock assigned to {gameObject.name}.");
            return 0f;
        }
    }
    // This method returns the list of modifiers currently applied to the stats.
    public List<StatModifier> GetModifiers()
    {
        return _modifiers;
    }

    // This method checks if a specific stat is present in the final values dictionary.
    public bool HasStat(StatDefinition stat)
    {
        return _finalValues.ContainsKey(stat);
    }

    // This method adds a new modifier to the list and recalculates the affected stat to reflect the change in its final value.
    public void AddModifier(StatModifier modifier)
    {
        _modifiers.Add(modifier);
        Recalculate(modifier.Stat);
    }

    // This method removes a specific modifier from the list and recalculates the affected stat.
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
            Debug.Log($"Modifier {modifier} removed from {gameObject.name}. Recalculated stat {modifier.Stat.name}.");
        }
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
            RemoveModifier(modifiersToRemove[i]);
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
            if (!_modifiers[i].Permanent && _modifiers[i].Duration <= 0f)
            {
                expiredModifiers.Add(_modifiers[i]);
            }
            else
            {
                _modifiers[i].Duration -= Time.deltaTime;
            }
        }

        for (int i = 0; i < expiredModifiers.Count; i++)
        {
            RemoveModifier(expiredModifiers[i]);
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

            }
            float finalValue = (baseValue + additiveValue) * multiplicativeValue;
            finalValue = stat.Clamp(finalValue);
            _finalValues[stat] = finalValue;
            OnStatChanged?.Invoke(_statBlock.Entries[i].Stat, _finalValues[_statBlock.Entries[i].Stat]);
            Debug.Log($"Final value for {stat.name} recalculated: {finalValue}");
        }
    }

    // This method initializes the final values dictionary by iterating through the stat entries in the stat block and calculating their final values.
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
