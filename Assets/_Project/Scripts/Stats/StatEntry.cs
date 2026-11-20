using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>One row of a StatBlock: a stat and its base value before modifiers.</summary>
[Serializable]
public class StatEntry
{
    [SerializeField, FormerlySerializedAs("Stat")] private StatDefinition _stat;
    [SerializeField, FormerlySerializedAs("BaseValue")] private float _baseValue;

    public StatDefinition Stat => _stat;
    public float BaseValue => _baseValue;
}
