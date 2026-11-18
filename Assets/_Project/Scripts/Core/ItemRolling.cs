using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;


public class ItemRolling : MonoBehaviour
{

    [Tooltip("The gun parts this room can offer when cleared.")]
    [SerializeField]
    private RoomCompletionLootPool _lootPool;
    
    public RoomCompletionLootPool LootPool { get => _lootPool; }
    
    // Reward options generated when the room completes and displayed by RewardScreen
    private List<WeightedReward<GunPart>> rewardOptions = new();
    
    public IReadOnlyList<WeightedReward<GunPart>> RewardOptions => rewardOptions; 
    
    private bool _complete = false;
    
    private List<WeightedReward<GunPart>> OnComplete(bool force = false)
    {
        
        if ((_roomType == RoomType.Combat || force) && _lootPool != null) 
        {
            List<WeightedReward<GunPart>> rewards = _lootPool.PickThree();

            for (int i = 0; i < rewards.Count; i++)
            {
                if (rewards[i].Item == null)
                    continue;

                Debug.Log($"Completion Reward {i + 1}:" +
                          $" {rewards[i].Item.PartName} with rarity {rewards[i].Weight}");
            }
                
            rewardOptions = rewards;
        }
        
        return rewardOptions; //never returns a null but now it'll return an empty list which is checkable and functionally the same
    }
    
    /// <summary>
    /// Debug: picks one of the parts the last OnComplete rolled, and returns the other two to the loot pool.
    /// Doesn't equip anything. Temporary until the reward screen exists (task 7).
    /// </summary>
    /// <param name="reward">0, 1, or 2.</param>
    // INTEGRATION: the 1, 2, and 3 debug keys became this Inspector button.
    [Button, DisableInEditorMode]
    public void PickReward(int reward) //made public
    {
        if (rewardOptions == null || reward >= rewardOptions.Count)
            return;

        for (int i = 0; i < rewardOptions.Count; i++)
        {
            if (i == reward)
            {
                Debug.Log($"Picked Reward {i + 1}");
                continue;
            }

            _lootPool.Obtained.Remove(rewardOptions[i]);
        }
    }
    
    
}
