using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Scripts.Dungeon
{
    /// <summary>
    /// Represents a room, attach this script to a
    /// room prefab.
    /// </summary>
    public class Room : MonoBehaviour
    {
        /// <summary>
        /// The room types
        /// </summary>
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
        
        public RoomCompletionLootPool  LootPool { get => _lootPool; }
        
        [SerializeField]
        private GameObject _playerSpawn; // This needs to be set in the editor

        [SerializeField]
        private BoxCollider _boundingBox;

        public BoxCollider BoundingBox { get => _boundingBox; }
        
        [Header("Debug (Do not edit)")] 
        [SerializeField]
        private string _id;

        [SerializeField]
        private bool _complete;

        [SerializeField]
        private Portal _portal;
        
        public Portal Portal { get => _portal; }
        
        [SerializeField] 
        private Room _north;

        public Vector3 PlayerSpawnPos
        {
            get { return _playerSpawn.transform.position + new Vector3(0f, 20f, 0f); }
        }
        
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
        
        // A unique ID for the GameManager (?)
        public string Id { get => _id; set => _id = value; }
        
        public bool Complete { get => _complete; }
        
        // This should possibly be temporary
        private List<WeightedReward<GunPart>> rewardOptions = new();

        private void Awake()
        {
            _portal = GetComponentInChildren<Portal>();

            if (_portal == null)
                Debug.Log($"{_id} has no exit portal");
            else
                _portal.Parent = this;
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

        // This should suffice for getting room completion rewards
        public List<WeightedReward<GunPart>> OnComplete(bool force = false)
        {
            _complete = true;
            _portal.Active = true;

            if (_roomType == RoomType.Combat || force)
            {
                List<WeightedReward<GunPart>> rewards = _lootPool.PickThree();
                
                //Debug.Log($"Rolled: {rewards.Count}");

                for (int i = 0; i < rewards.Count; i++)
                {
                    if (rewards[i].Item == null)
                        continue;
                    
                    Debug.Log($"{_id} Completion Reward {i + 1}:" +
                              $" {rewards[i].Item.PartName} with rarity {rewards[i].Weight}");
                }
                
                rewardOptions = rewards;
                return rewards;
            }

            return null;
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
