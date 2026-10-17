using UnityEngine;

public class StatModifier
{
    private float value;
    private modifierType type;
    private string source;


    public StatModifier(float value, modifierType type, string source)
    {
        this.value = value;
        this.type = type;
        this.source = source;
    }


}

public enum modifierType {Multiplicative, Additive}