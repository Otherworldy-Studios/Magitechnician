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
            // Reason for Error: The StatBlock script's variable is an array of type String,
            // Fix for Error: The StatBlock script's variable and method should actually be an array of type Stat instead
            foreach (Stat stat in statBlock.GetStats())
            {
                currentStats[stat.statID] = new StatRuntime(stat);
            }
        }
    }

    // Reason for Errors: 'stat' is undefined within the scope of each method and the greater StatsRuntime class
    // Fix for Errors: Instead of this, directly call currentStats[statID].AddModifier(modifier.modifierID, modifier);
    //                 Do this for each subsequent method, calling the correct respective method and arguments from StatRuntime
    public void AddModifierToStat(string statID, StatModifier modifier)
    {
        StatRuntime stat = currentStats[statID];
        stat.AddModifier(modifier);
    }

    public void RemoveModifierFromStat(string statID, string modifierID)
    {
        stat = currentStats[statID];
        stat.RemoveModifier(modifierID);
    }

    // Similar to the previous error, but instead return currentStats[statID].GetFinalValue(statID);
    public float GetFinalValue(string statID)
    {
        stat = currentStats[statID];
        return statID.GetFinalValue();
    }


}
