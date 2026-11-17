using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// A change to one stat, such as Damage Add 5 or FireRate Multiply 1.2. Items hold lists of these in their assets.
/// Stats never stores an item's own modifier: AddModifier stores a copy made with CopyWithSource, so counting down
/// a timed copy never changes the asset.
/// </summary>
[Serializable]
public class StatModifier
{
    // The JSON item importer writes these fields by name: _stat, _operation, _value, _duration, and _permanent.
    [Tooltip("The stat this changes.")]
    [SerializeField, FormerlySerializedAs("Stat")] private StatDefinition _stat;
    [Tooltip("Add, Multiply, or Set.")]
    [SerializeField, FormerlySerializedAs("Operation")] private ModifierOperation _operation;
    [Tooltip("The amount. For Multiply, the factor: 1.2 is +20%.")]
    [SerializeField, FormerlySerializedAs("Value")] private float _value;
    [Tooltip("Seconds the modifier lasts when Permanent is off. A non-permanent modifier with 0 is removed on the next frame.")]
    [SerializeField, FormerlySerializedAs("Duration")] private float _duration;
    [Tooltip("Never expires. Turn on for gun parts, charms, and consumables (Inventory times consumables itself).")]
    [SerializeField, FormerlySerializedAs("Permanent")] private bool _permanent;

    public StatDefinition Stat => _stat;
    public ModifierOperation Operation => _operation;
    public float Value => _value;
    /// <summary>Seconds left. On a copy in Stats, this counts down.</summary>
    public float Duration => _duration;
    public bool Permanent => _permanent;

    // INTEGRATION: the item (or other object) that added this copy. Set by CopyWithSource, so RemoveAllFromSource
    // can remove everything one item added. Not serialized.
    /// <summary>The object that added this modifier, such as the gun part or charm.</summary>
    public object Source { get; private set; }

    /// <summary>
    /// Returns a new modifier with the same values and the given source. Stats.AddModifier stores the copy,
    /// so the asset's own modifier is never changed.
    /// </summary>
    /// <param name="source">The item or object adding the modifier.</param>
    public StatModifier CopyWithSource(object source)
    {
        return new StatModifier
        {
            _stat = _stat,
            _operation = _operation,
            _value = _value,
            _duration = _duration,
            _permanent = _permanent,
            Source = source
        };
    }

    /// <summary>
    /// Counts the duration down by deltaTime. Returns true when the modifier has run out.
    /// A permanent modifier never runs out.
    /// </summary>
    /// <param name="deltaTime">Seconds since the last frame.</param>
    public bool Tick(float deltaTime)
    {
        if (_permanent) return false;

        _duration -= deltaTime;
        return _duration <= 0f;
    }
}
