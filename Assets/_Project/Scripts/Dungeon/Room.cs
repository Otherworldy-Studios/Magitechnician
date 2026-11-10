using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public RoomCompletionLootPool LootPool { get => _lootPool; }

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
        private List<Spawnpoint> _spawnpoints;
        
        public List<Spawnpoint> Spawnpoints { get => _spawnpoints; }

        [SerializeField]
        private Portal _portal;

        public Portal Portal { get => _portal; }

        [SerializeField]
        private Room _north;

        public Vector3 PlayerSpawnPos
        {
            get { return _playerSpawn.transform.position; }
        }

        // CHANGED: new. The player faces the spawn point's blue (Z) axis after a teleport.
        public Quaternion PlayerSpawnRotation => _playerSpawn.transform.rotation;

        public RoomType Type => _roomType; // CHANGED: new

        // CHANGED: new. EnterRoom fires OnRoomEntered. OnComplete fires OnRoomCleared once.
        // RoomEncounter listens to OnRoomEntered. Room rewards can listen to OnRoomCleared later.
        public event Action<Room> OnRoomEntered;
        public event Action<Room> OnRoomCleared;

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
                Debug.Log($"{name} has no exit portal"); 
            else
                _portal.Parent = this;

            _spawnpoints = GetComponentsInChildren<Spawnpoint>().ToList();
        }
        
        public void EnterRoom()
        {
            if (GameManager.HasInstance)
                GameManager.Instance.ChangeRoom(this);

            OnRoomEntered?.Invoke(this);
        }


        [Button, DisableInEditorMode]

        public List<WeightedReward<GunPart>> OnComplete(bool force = false)
        {
            bool wasComplete = _complete; // CHANGED
            _complete = true;

          
            if (_portal != null && _portal.Connected != null)
                _portal.Active = true;

            if (!wasComplete)                    // CHANGED
                OnRoomCleared?.Invoke(this);     // CHANGED

            if ((_roomType == RoomType.Combat || force) && _lootPool != null) 
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
        [Button, DisableInEditorMode]
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