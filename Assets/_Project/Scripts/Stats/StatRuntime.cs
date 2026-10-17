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

        foreach (var modifier in statModifiers)
        {
            if (statModifiers[modifier].type == additive)
            {
                additive += statModifiers[modifier].value;
            }
            else
            {
                multiplier *= statModifiers[modifier].value;
            }
        }

        return (GetBaseValue(stat) + additive) * multiplier;
    }

    
    private float GetBaseValue(string statID)
    {
        foreach (var s in stats)
        {
            if (s.StatID == statID)
                return s.BaseValue;
        }

    }
}