using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Stat", menuName = "Stats/Stat")]
public class Stat : ScriptableObject
{
    public float value;
    public string statID;
    

    public float GetStatValue()
    {
        return this.value;
    }
}

