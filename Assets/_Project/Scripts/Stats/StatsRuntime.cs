using UnityEngine;
using System.Collections.Generic;

public class StatsRuntime : MonoBehaviour
{
    [SerializeField] private StatBlock statBlock;
    private Dictionary<string, StatRuntime> currentStats = new();

    private void InitializeStats()
    {
        if (statBlock != null)
        {
            foreach (Stat stat in statBlock.GetStats())
            {
                currentStats[stat.statID] = new StatRuntime();
            }
        }
    }

    public void AddModifierToStat(string statID, StatModifier modifier)
    {
        currentStats[statID].AddModifier(modifier.modifierID, modifier);
    }

    public void RemoveModifierFromStat(string statID, string modifierID)
    {
        currentStats[statID].RemoveModifier(modifierID);
    }

    public float GetFinalValue(string statID)
    {
        return currentStats[statID].GetFinalValue(statID);
    }
}
