using System;

[Serializable]
public class StatModifier
{
    public float value;
    public modifierType type;
    public string source;
    public string modifierName;


    public StatModifier(float value, modifierType type, string source, string modifierName)
    {
        this.value = value;
        this.type = type;
        this.source = source;
        this.modifierName = modifierName;
    }


}

public enum modifierType {Multiplicative, Additive}