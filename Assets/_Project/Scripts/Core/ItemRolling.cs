using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rolls the gun parts offered when a combat room is cleared, and remembers which parts the player took this run
/// so they aren't offered again. GameManager calls RollRewards, PickReward, and ResetObtained.
/// </summary>
public class ItemRolling : MonoBehaviour
{
    [SerializeField] private RoomCompletionLootPool _lootPool;

    private List<WeightedReward<GunPart>> _rewardOptions = new();
    private readonly HashSet<GunPart> _obtained = new();

    
    public IReadOnlyList<WeightedReward<GunPart>> RewardOptions => _rewardOptions;

    /// <summary>
    /// Rolls up to three parts. Skips parts taken this run and parts the gun has equipped right now.
    /// Returns an empty list when nothing is left to offer.
    /// </summary>
    /// <param name="gun">The player's gun. Can be null, which skips the equipped check.</param>
    public IReadOnlyList<WeightedReward<GunPart>> RollRewards(Gun gun)
    {
        _rewardOptions = new List<WeightedReward<GunPart>>();

        if (_lootPool == null)
        {
            Debug.LogError("ItemRolling has no loot pool assigned.", this);
            return _rewardOptions;
        }

        HashSet<GunPart> excluded = new(_obtained);

        if (gun != null)
        {
            foreach (GunPartType type in Enum.GetValues(typeof(GunPartType)))
            {
                GunPart equipped = gun.GetEquippedPart(type);
                if (equipped != null)
                {
                    excluded.Add(equipped);
                }
            }
        }

        _rewardOptions = _lootPool.PickThree(excluded);
        return _rewardOptions;
    }

    /// <summary>Marks the selected part as obtained and clears the roll. The parts left over go back into the pool.</summary>
    public void PickReward(int index)
    {
        if (index < 0 || index >= _rewardOptions.Count) return;

        _obtained.Add(_rewardOptions[index].Item);
        _rewardOptions.Clear();
    }

    /// <summary>Puts every part back in the pool. GameManager calls this when a run starts.</summary>
    public void ResetObtained()
    {
        _rewardOptions.Clear();
        _obtained.Clear();
    }
}