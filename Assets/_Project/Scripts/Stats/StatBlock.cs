using System.Collections.Generic;
using UnityEngine;

// This class is a ScriptableObject that holds a list of StatEntry objects, which represent individual stats and their values.
[CreateAssetMenu(fileName = "StatBlock", menuName = "Stats/Stat Block")]
public class StatBlock : ScriptableObject
{
    public List<StatEntry> Entries;
}
