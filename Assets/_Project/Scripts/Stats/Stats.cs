using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;


// This class is a MonoBehaviour that manages a collection of stats and their modifiers, allowing for the calculation of final
// stat values based on base values and applied modifiers.
public class Stats : MonoBehaviour
{
    [SerializeField] private StatBlock _statBlock;
    [SerializeField] private List<StatModifier> _modifiers;
    [ShowInInspector, ReadOnly]
    private Dictionary<StatDefinition, float> _finalValues;
    public event Action<StatDefinition, float> OnStatChanged;
    public StatDefinition testStat;

    public StatModifier TestModifier;



    // This method is called when the script instance is being loaded. It initializes the stats by calling the InitializeStats method.
    private void Awake()
    {
        InitializeStats();
    }

    private void Update()
    {

        RemoveExpiredModifiers();
    }

    [Button, DisableInEditorMode]
    private void AddTestModifier()
    {
        Debug.Log($"Adding modifier {TestModifier} to {gameObject.name}.");
        AddModifier(TestModifier, this); 
    }
    [Button, DisableInEditorMode]
    private void RemoveTestModifier()
    {
        RemoveAllFromSource(this); 
    }
    [Button, DisableInEditorMode]
    private void GetTestStatValue()
    {
        GetValue(testStat);
    }

   
    [Button, DisableInEditorMode]
    public void ClearAllModifiers()
    {
        _modifiers.Clear();
        for (int i = 0; i < _statBlock.Entries.Count; i++)
        {
            Recalculate(_statBlock.Entries[i].Stat);
        }
    }



    // This method retrieves the final value of a specific stat. If the stat is not present in the final values dictionary, it logs an error and returns 0.
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
    // This method returns the list of modifiers currently applied to the stats.
    public List<StatModifier> GetModifiers()
    {
        return _modifiers;
    }

    // This method checks if a specific stat is present in the final values dictionary.
    public bool HasStat(StatDefinition stat)
    {
        return stat != null && _finalValues.ContainsKey(stat); // CHANGED: a null stat returns false instead of throwing
    }

    // CHANGED: AddModifier now takes the object that added the modifier (a gun part, charm, or consumable).
    // It stores a copy of the modifier with Source set, so the asset's own modifier is never changed,
    // and RemoveAllFromSource(source) can find it later. It returns the copy.
    // source is optional so existing calls still compile, but without it RemoveAllFromSource can't find the modifier.
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

        StatModifier copy = new StatModifier                                            
        {                                                                               
            Stat = modifier.Stat,                                                       
            Operation = modifier.Operation,                                             
            Value = modifier.Value,                                                     
            Duration = modifier.Duration,                                               
            Permanent = modifier.Permanent,                                             
            Source = source                                                             
        };                                                                              

        _modifiers.Add(copy);                                                          
        Recalculate(copy.Stat);
        return copy;                                                                    
    }

    
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
        if (source == null) return; 

       
        for (int i = _modifiers.Count - 1; i >= 0; i--)
        {
            StatModifier modifier = _modifiers[i];      
            if (modifier.Source != source) continue;   

            _modifiers.RemoveAt(i);                     
            Recalculate(modifier.Stat);                 
        }
    }

    // This method updates the duration of each modifier based on the time elapsed since the last frame and removes any that have
    // expired.
    private void RemoveExpiredModifiers()
    {
        
        // Each expired modifier recalculates once instead of twice.
        for (int i = _modifiers.Count - 1; i >= 0; i--)
        {
            StatModifier modifier = _modifiers[i];      
            if (modifier.Permanent) continue;           

            modifier.Duration -= Time.deltaTime;        
            if (modifier.Duration > 0f) continue;       

            _modifiers.RemoveAt(i);                   
            Recalculate(modifier.Stat);               
        }
    }

    // This method recalculates the final value of a specific stat based on its base value and any applied modifiers. It takes
    // into account both additive and multiplicative modifiers and clamps the final value within the defined min/max range for
    // the stat.
    private void Recalculate(StatDefinition stat)
    {
        if (!HasStat(stat)) return; 

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

        foreach (StatModifier modifier in _modifiers)
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
        finalValue = stat.Clamp(finalValue);

        _finalValues.TryGetValue(stat, out float oldValue);
        _finalValues[stat] = finalValue;

        if (!Mathf.Approximately(oldValue, finalValue))
        {
            OnStatChanged?.Invoke(stat, finalValue);
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