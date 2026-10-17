using System;

[Serializable]
public class StatModifier
{
    public float value;
    public modifierType type;
    public string source;
    public string modifierID;


    public StatModifier(float value, modifierType type, string source, string modifierID)
    {
        this.value = value;
        this.type = type;
        this.source = source;
        this.modifierID = modifierID;
    }


}

public enum modifierType {Multiplicative, Additive}