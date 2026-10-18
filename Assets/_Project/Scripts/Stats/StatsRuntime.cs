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
                currentStats[stat.statID] = new StatRuntime(stat);
            }
        }
    }

    public void AddModifierToStat(string statID, StatModifier modifier)
    {
        stat = currentStats[statID];
        stat.AddModifier(modifier);
    }

    public void RemoveModifierFromStat(string statID, string modifierID)
    {
        stat = currentStats[statID];
        stat.RemoveModifier(modifierID);
    }

    public float GetFinalValue(string statID)
    {
        stat = currentStats[statID];
        return statID.GetFinalValue();
    }


}
