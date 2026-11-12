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
    private List<WeightedReward<GunPart>> _rewards;

    [NonSerialized] //CHANGED: unity tries less to save this to the data asset
    private List<WeightedReward<GunPart>> _obtained = new();
    
    public List<WeightedReward<GunPart>> Obtained { get { return _obtained; } }

    public void ResetObtained()
    {
        _obtained.Clear();
    }
    
    // This gets really weird when there are multiple rooms rolling at once
    private List<WeightedReward<GunPart>> _rewards; // Every part this pool can give.

    [SerializeField]
    private List<WeightedReward<GunPart>> _obtained; // Parts already rolled. Clear this by hand in the Inspector.

    /// <summary>Parts already rolled. Room.PickReward removes the parts that weren't picked.</summary>
    public List<WeightedReward<GunPart>> Obtained { get { return _obtained; } }

    /// <summary>
    /// Rolls up to three parts that haven't been obtained, weighted by rarity, and adds them to Obtained.
    /// Returns fewer than three when the pool runs low. Results get unreliable when several rooms roll at once,
    /// because they share Obtained.
    /// </summary>
    public List<WeightedReward<GunPart>> PickThree()
    {
        List<WeightedReward<GunPart>> rolledRewards = new List<WeightedReward<GunPart>>();
        ItemSelector<WeightedReward<GunPart>> rewardPool = new ItemSelector<WeightedReward<GunPart>>();

        Debug.Log($"Items: {_rewards.Count}");

        // Only parts not yet obtained go into the roll.
        foreach (WeightedReward<GunPart> part in _rewards)
        {
            if (_obtained.Contains(part))
            {
                Debug.Log($"{part.Item.PartName} is already obtained");
                continue;
            }

            rewardPool.AddItem(part, (long)part.Weight);
            Debug.Log($"Added {part.Item.PartType}");
        }

        // RollWithRemoval takes each rolled part out of the pool, so the three are different.
        for (int i = 0; i < 3 && _obtained.Count < _rewards.Count; i++)
        {
            WeightedReward<GunPart> roll = rewardPool.RollWithRemoval();

            if (roll.Item == null)
                continue;

            if (_obtained.Contains(roll))
                continue;

            rolledRewards.Add(roll);
            _obtained.Add(roll);
        }

        return rolledRewards;
    }
}
