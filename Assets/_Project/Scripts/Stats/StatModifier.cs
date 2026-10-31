using System;
using UnityEngine;

// This class represents a modifier that can be applied to a stat, including the stat it modifies, the operation to perform, 
// the value of the modifier, the source of the modifier, and its duration.
[Serializable]
public class StatModifier
{
    public StatDefinition Stat;
    public ModifierOperation Operation;
    public float Value;
    public object Source;
    public float Duration;

}