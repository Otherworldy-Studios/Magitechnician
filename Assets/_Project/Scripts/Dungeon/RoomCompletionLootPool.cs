using System;
using _Project.Scripts.Dungeon.Utility;
using UnityEngine;

// An item and weight
[Serializable]
struct WeightedReward<T>
{
    [SerializeField]
    private T _item;
    
    [SerializeField]
    private RarityChance _weight;
    
    public T Item { get { return _item; } }
    public RarityChance Weight { get { return _weight; } }
}

enum RarityChance
{
    Common = 65,
    Uncommon = 20,
    Rare = 10,
    Epic = 5,
}

// This may need to be moved
public class RoomCompletionLootPool : ScriptableObject
{
    [SerializeField]
    private WeightedReward<GunPart>[] _rewards;

    public GunPart[] PickThree()
    {
        GunPart[] rolled = new GunPart[3];
        ItemSelector<GunPart> rewards = new ItemSelector<GunPart>();
        
        foreach(WeightedReward<GunPart> part in _rewards)
            rewards.AddItem(part.Item, (int)part.Weight);

        for (int i = 0; i < 3; i++)
            rolled[i] = rewards.RollWithRemoval();
        
        return rolled;
    }
}
