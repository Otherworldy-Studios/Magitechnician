using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;


public class ItemRolling : MonoBehaviour
{
    [SerializeField] private RoomCompletionLootPool _lootPool;

    private List<WeightedReward<GunPart>> _rewardOptions = new();
    public IReadOnlyList<WeightedReward<GunPart>> RewardOptions => _rewardOptions;
    
    private HashSet<GunPart> _obtained = new();

    // Generates rewards and returns them to whoever requested them.
    public IReadOnlyList<WeightedReward<GunPart>> RollRewards()
    {
        _rewardOptions = new List<WeightedReward<GunPart>>();

        if (_lootPool == null)
        {
            Debug.LogError("ItemRolling has no loot pool assigned.");
            return _rewardOptions;
        }

        _rewardOptions = _lootPool.PickThree(_obtained);

        return _rewardOptions;
    }

    // Resets the pool when a new run begins.
    public void ResetObtained()
    {
        _rewardOptions.Clear();
        _obtained.Clear();
    }

    // Keeps the selected reward and returns the others to the pool.
    public void PickReward(int index)
    {
        if (index < 0 || index >= _rewardOptions.Count)
        {
            return;
        }

        GunPart selectedPart = _rewardOptions[index].Item;

        
        
        _obtained.Add(selectedPart);

        _rewardOptions.Clear();
    }
}

