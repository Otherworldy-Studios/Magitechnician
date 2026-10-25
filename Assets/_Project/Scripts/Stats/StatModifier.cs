using System;
using UnityEngine;

// Multiplicative and Additive modifiers to stats in addition to having them be temporary/permenant
[Serializable]
public class StatModifier
{
    public float value;
    public modifierType type;
    public string modifierID;
    public float? expiresAt;
    public bool isPermanent => !expiresAt.HasValue;

    public StatModifier(float value, modifierType type, string modifierID, float? duration)
    {
        this.value = value;
        this.type = type;
        this.modifierID = modifierID;
        
        if (!duration.HasValue)
        {
            return;
        }

        expiresAt = Time.time + duration.Value;
    }

}

public enum modifierType {Multiplicative, Additive}