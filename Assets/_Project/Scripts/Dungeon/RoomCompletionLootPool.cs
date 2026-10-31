using System;
using System.Collections.Generic;
using _Project.Scripts.Dungeon.Utility;
using UnityEngine;

// An item and weight
[Serializable]
public struct WeightedReward<T>
{
    [SerializeField]
    private T _item;
    
    [SerializeField]
    private RarityChance _weight;
    
    public T Item { get { return _item; } }
    public RarityChance Weight { get { return _weight; } }
}

public enum RarityChance
{
    Common = 65,
    Uncommon = 20,
    Rare = 10,
    Epic = 5,
}

// This will  appear in the right click menu in the Unity Project tab under
// Create -> Dungeon -> Room Completion Loot Pool
[CreateAssetMenu(menuName = "Dungeon/Room Completion Loot Pool")]
public class RoomCompletionLootPool : ScriptableObject
{
    [SerializeField]
    private List<WeightedReward<GunPart>> _rewards;
    
    [SerializeField]
    private List<WeightedReward<GunPart>> _obtained; // This needs to be wiped manually in the inspector
    
    public List<WeightedReward<GunPart>> Obtained { get { return _obtained; } }

    // This gets really weird when there are multiple rooms rolling at once
    public List<WeightedReward<GunPart>> PickThree()
    {
        List<WeightedReward<GunPart>> rolledRewards = new List<WeightedReward<GunPart>>();
        ItemSelector<WeightedReward<GunPart>> rewardPool = new ItemSelector<WeightedReward<GunPart>>();

        Debug.Log($"Items: {_rewards.Count}");
        
        foreach (WeightedReward<GunPart> part in _rewards)
        {
            if (_obtained.Contains(part))
            {
                Debug.Log($"{part.Item.PartName} is already obtained");
                continue;
            }

            //Debug.Log($"{part} weight is {(int)part.Weight}");
            rewardPool.AddItem(part, (long)part.Weight);
            Debug.Log($"Added {part.Item.PartType}");
        }

        for (int i = 0; i < 3 && _obtained.Count < _rewards.Count; i++)
        {
            WeightedReward<GunPart> roll = rewardPool.RollWithRemoval();
            
            if(roll.Item == null)
                continue;
            
            if(_obtained.Contains(roll))
                continue;

            rolledRewards.Add(roll);
            _obtained.Add(roll);
        }

        return rolledRewards;
    }
}
