using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "StatBlock", menuName = "Scriptable Objects/StatBlock")]

[System.Serializable]
public class StatBlock : ScriptableObject
{
    private string[] stats;

    public string[] GetStats()
    {
        return stats;
    }
}
