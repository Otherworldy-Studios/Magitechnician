using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Scripts.Dungeon
{
    public class Room : MonoBehaviour
    {
        public enum RoomType
        {
            Start,
            Combat,
            End
        }

        [Header("Room Settings")] 
        [SerializeField]
        private RoomType _roomType;
        
        [SerializeField]
        private RoomCompletionLootPool _lootPool;

        [Header("Debug (Do not edit)")] 
        [SerializeField]
        private string _id;

        [SerializeField]
        private bool _complete;
        
        [SerializeField] 
        private Room _north;

        public Room North
        {
            get => _north;
            set => _north = value;
        }

        [SerializeField] private Room _south;

        public Room South
        {
            get => _south;
            set => _south = value;
        }

        [SerializeField] private Room _east;

        public Room East
        {
            get => _east;
            set => _east = value;
        }

        [SerializeField] private Room _west;

        public Room West
        {
            get => _west;
            set => _west = value;
        }
        
        public string Id { get => _id; set => _id = value; }
        
        public bool Complete { get => _complete; }
        
        // This should possibly be temporary
        private List<WeightedReward<GunPart>> rewardOptions;

        void Start()
        {
        }

        void Update()
        {
            // Temporary, get rid of this later
            #if UNITY_EDITOR
            if (Keyboard.current != null)
            {
                if (Keyboard.current.zKey.wasPressedThisFrame)
                    OnComplete();
                
                // Really truly terrible reward pick logic
                if(Keyboard.current.digit1Key.wasPressedThisFrame)
                    PickReward(0);
                if(Keyboard.current.digit2Key.wasPressedThisFrame) 
                    PickReward(1);
                if(Keyboard.current.digit3Key.wasPressedThisFrame)
                    PickReward(2);
            }
            #endif
        }

        void OnComplete()
        {
            _complete = true;

            if (_roomType == RoomType.Combat)
            {
                List<WeightedReward<GunPart>> rewards = _lootPool.PickThree();

                for (int i = 0; i < rewards.Count; i++)
                {
                    if (rewards[i].Item == null)
                        continue;
                    
                    Debug.Log($"{_id} Completion Reward {i + 1}:" +
                              $" {rewards[i].Item.PartName} with rarity {rewards[i].Weight}");
                }
                
                rewardOptions = rewards;
            }
        }

        // This should probably be temporary
        // Picks the reward at an index of options
        // This needs to just be a player pickup thing
        void PickReward(int reward)
        {
            if (rewardOptions == null || reward >= rewardOptions.Count)
                return;

            for (int i = 0; i < rewardOptions.Count; i++)
            {
                if (i == reward)
                {
                    Debug.Log($"{_id} Picked Reward {i + 1}");
                    continue;
                }

                _lootPool.Obtained.Remove(rewardOptions[i]);
            }
        }
    }
}
