using UnityEngine;

public enum statType
{
    speed,
    maxHealth,
    baseDamage
}

public enum modifierType
{
    add,
    multiply
}

//Every modifier records the object that applied it (Charm, Consummable)
//
public class EntityStatsModifier
{
    public statType Stat { get; }
    public modifierType Type { get; }
    public float Value { get; }
    public object Source { get; }

    //Time (in seconds) until the modifier expires. 0 = permanent.
    public float Duration { get; private set; }

    public bool isTemporary { get; } //set once in the constructor, true if Duration > 0
    public bool isExpired => isTemporary && Duration <= 0;


    public EntityStatsModifier(statType stat, modifierType type, float value, object source, float duration = 0)
    {
        Stat = stat;
        Type = type;
        Value = value;
        Source = source;
        Duration = duration;
        isTemporary = duration > 0;
    }

    public void UpdateDuration(float deltaTime)
    {
        if (isTemporary)
        {
            Duration -= deltaTime;
            if (Duration < 0)
            {
                Duration = 0;
            }
        }
    }

    public override string ToString()
    {
        string typeString = Type == modifierType.add ? $"{Value:+0.00;-0.00}" : $"x{Value:0.00}";
        string durationString = isTemporary ? $" (Duration: {Duration:0.00}s)" : " (permanent)";
        return $"{Stat} {typeString}{durationString} from {Source}";
    }
}

