using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// An asset that lists the stats an object has and their base values, for example StatBlock_Player or StatBlock_Gun.
/// A Stats component reads it in Awake. A stat that isn't listed here doesn't exist on that object, and modifiers for it are refused.
/// </summary>
[CreateAssetMenu(fileName = "StatBlock", menuName = "Stats/Stat Block")]
public class StatBlock : ScriptableObject
{
    [Tooltip("Each stat this object has, with its base value.")]
    [SerializeField, FormerlySerializedAs("Entries")] private List<StatEntry> _entries;

    /// <summary>Each stat this object has, with its base value.</summary>
    public IReadOnlyList<StatEntry> Entries => _entries;
}
