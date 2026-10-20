using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "StatBlock", menuName = "Scriptable Objects/StatBlock")]

[System.Serializable]
public class StatBlock : ScriptableObject
{
    private Stat[] stats;

    public Stat[] GetStats()
    {
        return stats;
    }
}
