using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// How much stronger enemies get as the player goes deeper. Each tier is a list of stat modifiers that WaveManager adds
/// to every enemy it spawns, picked by how many rooms the player has cleared this run. Tier 0 is the first combat room.
/// Rooms past the last tier use the last tier. The modifiers change the enemy's stats, and its attacks read those
/// stats, so raising AttackDamage or lowering WindUpTime and CooldownTime also strengthens every attack.
/// </summary>
[CreateAssetMenu(fileName = "DifficultyScaling", menuName = "Dungeon/Waves/Difficulty Scaling")]
public class DifficultyScaling : ScriptableObject
{
    [Serializable]
    public class Tier
    {
        [Tooltip("Added to every enemy spawned at this depth. Use Multiply to scale a stat, such as MaxHealth by 1.25.")]
        [SerializeField] private List<StatModifier> _modifiers = new();

        public IReadOnlyList<StatModifier> Modifiers => _modifiers;
    }

    [Tooltip("One tier per room depth, starting with the first combat room. Leave empty for no scaling.")]
    [ListDrawerSettings(ShowIndexLabels = true)]
    [SerializeField] private List<Tier> _tiers = new();

    /// <summary>The modifiers for this depth, using the last tier past the end. Empty when there are no tiers.</summary>
    public IReadOnlyList<StatModifier> GetModifiers(int roomsCleared)
    {
        if (_tiers.Count == 0) return Array.Empty<StatModifier>();

        int index = Mathf.Clamp(roomsCleared, 0, _tiers.Count - 1);
        return _tiers[index].Modifiers;
    }
}