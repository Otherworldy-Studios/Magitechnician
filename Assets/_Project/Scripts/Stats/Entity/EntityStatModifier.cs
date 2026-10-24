using System;
using UnityEngine;

[Serializable]
public class EntityStatModifier : StatModifier
{
    public EntityStatType entityStatType;
    
    public EntityStatModifier(float value, modifierType type, string modifierID, float? duration, EntityStatType entityStatType) : base(value, type, modifierID, duration)
    {
        this.entityStatType = entityStatType;
    }

    public bool IsExpired()
    {
        return expiresAt.HasValue && Time.time >= expiresAt.Value;
    }
}
