using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Stats", menuName = "Scriptable Objects/Stats")]
public class Stats : ScriptableObject
{
    public Dictionary<string, Stat> stats = new Dictionary<string, Stat>();

    public void AddStat(string statName, Stat stat)
    {
        stats[statName] = stat;
    }

    public void RemoveStat(string statName)
    {
        stats.Remove(statName);
    }
}
