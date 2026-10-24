using System.Collections.Generic;
using UnityEngine;

public class StatRuntime : MonoBehaviour
{
    public List<Stat> stats = new();
    public Dictionary<string, StatModifier> statModifiers = new Dictionary<string, StatModifier>();
    
    public void AddModifier(string modifierID, StatModifier modifier)
    {
        statModifiers[modifierID] = modifier;
    }

    public void RemoveModifier(string modifierID)
    {
        statModifiers.Remove(modifierID);
    }
    
    public float GetFinalValue(string statID)
    {
        float additive = 0f;     
        float multiplier = 1f;

        foreach (var (modifierID, modifier) in statModifiers)
        {
            if (statModifiers[modifierID].type == modifierType.Additive)
            {
                additive += statModifiers[modifierID].value;
            }
            else
            {
                multiplier *= statModifiers[modifierID].value;
            }
        }

        return (GetBaseValue(statID) + additive) * multiplier;
    }

    private float GetBaseValue(string statID)
    {
        foreach (var s in stats)
        {
            if (s.statID == statID)
                return s.GetStatValue();
        }
        return 0f;
    }
}