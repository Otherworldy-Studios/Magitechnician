using System;
using System.Collections.Generic;
using _Project.Scripts.Dungeon.Utility;
using UnityEngine;

/// <summary>An item paired with the rarity that sets how likely it is to be rolled.</summary>
[Serializable]
public struct WeightedReward<T>
{
    [SerializeField] private T _item;
    [SerializeField] private RarityChance _weight;

    public T Item => _item;

    /// <summary>The entry's rarity. Its number value is its weight in the roll.</summary>
    public RarityChance Weight => _weight;
}

/// <summary>Rarities and their roll weights. Higher weights are rolled more often.</summary>
public enum RarityChance
{
    Common = 65,
    Uncommon = 20,
    Rare = 10,
    Epic = 5,
}

/// <summary>
/// The gun parts a cleared combat room can offer. ItemRolling calls PickThree and keeps track of what was taken.
/// </summary>
[CreateAssetMenu(menuName = "Dungeon/Room Completion Loot Pool")]
public class RoomCompletionLootPool : ScriptableObject
{
    [Tooltip("Every part this pool can give.")]
    [SerializeField] private List<WeightedReward<GunPart>> _rewards;

    /// <summary>
    /// Rolls up to three different parts, weighted by rarity, leaving out the excluded ones.
    /// Returns fewer than three when the pool runs low, and an empty list when it's out.
    /// </summary>
    /// <param name="excluded">Parts that can't be rolled, such as parts already taken or equipped.</param>
    public List<WeightedReward<GunPart>> PickThree(IReadOnlyCollection<GunPart> excluded)
    {
        List<WeightedReward<GunPart>> rolledRewards = new();
        ItemSelector<WeightedReward<GunPart>> rewardPool = new();

        // Also collects each added part, so a part listed twice is only added once.
        HashSet<GunPart> skipped = new(excluded);
        int availableCount = 0;

        foreach (WeightedReward<GunPart> reward in _rewards)
        {
            if (reward.Item == null || skipped.Contains(reward.Item)) continue;

            rewardPool.AddItem(reward, (long)reward.Weight);
            skipped.Add(reward.Item);
            availableCount++;
        }

        int rollCount = Mathf.Min(3, availableCount);

        for (int i = 0; i < rollCount; i++)
        {
            rolledRewards.Add(rewardPool.RollWithRemoval());
        }

        return rolledRewards;
    }
}