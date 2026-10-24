using System;
using UnityEngine;

[Serializable]
public class EntityStatModifier : StatModifier
{
    public float? expiresAt;
    public bool isPermanent => !expiresAt.HasValue;
    
    public EntityStatModifier(float value, modifierType type, string modifierID, float? duration) : base(value, type, modifierID)
    {
        if (!duration.HasValue)
        {
            return;
        }

        expiresAt = Time.time + duration.Value;
    }

    public bool IsExpired()
    {
        return expiresAt.HasValue && Time.time >= expiresAt.Value;
    }
}
