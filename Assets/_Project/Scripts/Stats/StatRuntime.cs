using System;
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
            // Reason for Error: 'additive' is of type float and .type is modifierType, which is an enum.
            // Fix for Error: Instead of this, you should write out modifierType.Additive in place of 'additive'
            if (statModifiers[modifierID].type == additive)
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
        // Reason for Error: No base return statement given
        // Fix for Error: Do return 0f;
    }
}