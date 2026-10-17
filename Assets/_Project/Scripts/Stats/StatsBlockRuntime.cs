using UnityEngine;
using System.Collections.Generic;

public class StatsBlockRuntime : MonoBehaviour
{
    public Dictionary<string, Stat> stats = new Dictionary<string, Stat>();

    public void AddStat(string statID, Stat stat)
    {
        stats[statID] = stat;
    }

    public void RemoveStat(string statID)
    {
        stats.Remove(statID);
    }
}
