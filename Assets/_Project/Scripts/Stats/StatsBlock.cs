using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "StatsBlock", menuName = "Scriptable Objects/StatsBlock")]
public class StatsBlock : ScriptableObject
{
    public List<Stat> stats = new();
}