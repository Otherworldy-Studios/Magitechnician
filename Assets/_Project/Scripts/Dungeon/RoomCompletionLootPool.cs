using System;
using System.Collections.Generic;
using _Project.Scripts.Dungeon.Utility;
using UnityEngine;

/// <summary>An item paired with the rarity that sets how likely it is to be rolled.</summary>
[Serializable]
public struct WeightedReward<T>
{
    [SerializeField]
    private T _item;

    [SerializeField]
    private RarityChance _weight;

    /// <summary>The item this entry gives.</summary>
    public T Item { get { return _item; } }

    /// <summary>The entry's rarity. Its number value is its weight in the roll.</summary>
    public RarityChance Weight { get { return _weight; } }
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
/// The gun parts a room can give when cleared, and which ones have already been given.
/// Room.OnComplete calls PickThree. Create one from Create > Dungeon > Room Completion Loot Pool.
/// </summary>
[CreateAssetMenu(menuName = "Dungeon/Room Completion Loot Pool")]
public class RoomCompletionLootPool : ScriptableObject
{
    [SerializeField]
    private List<WeightedReward<GunPart>> _rewards; // Every part this pool can give.
    /// <summary>
    /// Rolls up to three parts that haven't been obtained, weighted by rarity, and adds them to Obtained.
    /// Returns fewer than three when the pool runs low.
    /// </summary>
    public List<WeightedReward<GunPart>> PickThree(IReadOnlyCollection<GunPart> obtained)
    {
        List<WeightedReward<GunPart>> rolledRewards = new List<WeightedReward<GunPart>>();
        ItemSelector<WeightedReward<GunPart>> rewardPool = new ItemSelector<WeightedReward<GunPart>>();

        HashSet<GunPart> excluded = new(obtained);
        
        int available = obtained.Count;
        int availableCount = 0;

        foreach (WeightedReward<GunPart> part in _rewards)
        {
            if (part.Item == null || excluded.Contains(part.Item))
            {
                continue;
            }

            rewardPool.AddItem(part, (long)part.Weight);
            
            excluded.Add(part.Item);
            availableCount++;
        }

        int rollCount = Mathf.Min(3, availableCount);

        for (int i = 0; i < rollCount; i++)
        {
            WeightedReward<GunPart> roll = rewardPool.RollWithRemoval();

            rolledRewards.Add(roll);
        }

        return rolledRewards;
    }
    
}
