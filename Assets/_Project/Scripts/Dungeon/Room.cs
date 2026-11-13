using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Scripts.Dungeon
{
    /// <summary>
    /// One room of the dungeon. Goes on the root of a room prefab. Holds the room's type, loot pool, player spawn point,
    /// bounding box, exit portal, and links to its neighbors. EnterRoom runs when the player arrives, and OnComplete
    /// when the room is cleared, which opens the portal and rolls the rewards.
    /// </summary>
    public class Room : MonoBehaviour
    {
        /// <summary>What a room is for. The generator places one Start room, then Combat rooms, then one End room.</summary>
        public enum RoomType
        {
            Start,
            Combat,
            End
        }

        [Header("Room Settings")]
        [SerializeField]
        private RoomType _roomType;

        [Tooltip("The gun parts this room can offer when cleared.")]
        [SerializeField]
        private RoomCompletionLootPool _lootPool;

        public RoomCompletionLootPool LootPool { get => _lootPool; }

        [Tooltip("Where the player arrives. Its blue (Z) axis is the way the player faces after a teleport.")]
        [SerializeField]
        private GameObject _playerSpawn;

        [Tooltip("The room's size. The generator spaces rooms by its diagonal.")]
        [SerializeField]
        private BoxCollider _boundingBox;

        public BoxCollider BoundingBox { get => _boundingBox; }

        [Header("Debug (Do not edit)")]
        [SerializeField]
        private string _id; // Set by the generator, such as "Combat2".

        [SerializeField]
        private bool _complete; // True once OnComplete has run.

        [SerializeField]
        private List<Spawnpoint> _spawnpoints;
        
        public IReadOnlyList<Spawnpoint> Spawnpoints { get => _spawnpoints; }

        [SerializeField]
        private Portal _portal; // The exit. Found in children in Awake.

        public Portal Portal { get => _portal; }

        // Neighbor links, set by the generator. Null where there's no room.
        [SerializeField]
        private Room _north;

        /// <summary>Where the player arrives in this room.</summary>
        public Vector3 PlayerSpawnPos
        {
            get { return _playerSpawn.transform.position; }
        }

        /// <summary>Which way the player faces on arrival. PlayerSpawner.TeleportToRoom uses its yaw.</summary>
        // INTEGRATION: new, for PlayerSpawner.
        public Quaternion PlayerSpawnRotation => _playerSpawn.transform.rotation;

        public RoomType Type => _roomType;

        // INTEGRATION: new. RoomEncounter listens to OnRoomEntered to turn on its enemies, and later the wave manager.
        // RoomRewards will listen to OnRoomCleared to open the reward screen (task 7).
        /// <summary>Fires each time the player enters this room, with this room.</summary>
        public event Action<Room> OnRoomEntered;
        /// <summary>Fires once, the first time the room completes, with this room.</summary>
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

        /// <summary>A name for the room within this map, such as "Start1" or "Combat2". Set by the generator.</summary>
        public string Id { get => _id; set => _id = value; }

        /// <summary>True once the room is cleared.</summary>
        public bool Complete { get => _complete; }

        // The three parts rolled by the last OnComplete. Temporary until RoomRewards handles rewards (task 7).
        private List<WeightedReward<GunPart>> _rewardOptions = new();

        /// <summary>Finds the exit portal and tells it which room it belongs to.</summary>
        private void Awake()
        {
            _portal = GetComponentInChildren<Portal>();

            if (_portal == null)
                Debug.Log($"{name} has no exit portal"); // INTEGRATION: was _id, which the generator hasn't set yet in Awake.
            else
                _portal.Parent = this;

            _spawnpoints = GetComponentsInChildren<Spawnpoint>().ToList();
        }
        
        /// <summary>
        /// Marks this as the player's current room and fires OnRoomEntered. Portal calls this after moving the player in,
        /// and GameManager.StartRun calls it for the start room.
        /// </summary>
        // INTEGRATION: new. Calls GameManager.ChangeRoom, which replaces GameManager setting CurrentRoom itself.
        public void EnterRoom()
        {
            if (GameManager.HasInstance)
                GameManager.Instance.ChangeRoom(this);

            OnRoomEntered?.Invoke(this);
        }

        /// <summary>
        /// Clears the room: opens its portal if it leads somewhere, fires OnRoomCleared the first time, and rolls three
        /// parts from the loot pool for a Combat room or when forced. Returns the rolled parts, or null when nothing was rolled.
        /// Called by RoomEncounter when its enemies are dead, by LevelGenerator for a start room without enemies, and as a debug button.
        /// </summary>
        /// <param name="force">True rolls rewards for any room type. RoomEncounter passes its Roll Rewards option.</param>
        // INTEGRATION: the Z debug key became this Inspector button.
        [Button, DisableInEditorMode]
        public List<WeightedReward<GunPart>> OnComplete(bool force = false)
        {
            bool wasComplete = _complete; // INTEGRATION: new, so OnRoomCleared fires only once.
            _complete = true;

            // INTEGRATION: was "_portal.Active = true". Skips rooms without a portal, and leaves the End room's portal off,
            // because the End room's portal has no connected room.
            if (_portal != null && _portal.Connected != null)
                _portal.Active = true;

            if (!wasComplete)
                OnRoomCleared?.Invoke(this);

            if ((_roomType == RoomType.Combat || force) && _lootPool != null) // INTEGRATION: added the loot pool check.
            {
                List<WeightedReward<GunPart>> rewards = _lootPool.PickThree();

                for (int i = 0; i < rewards.Count; i++)
                {
                    if (rewards[i].Item == null)
                        continue;

                    Debug.Log($"{_id} Completion Reward {i + 1}:" +
                              $" {rewards[i].Item.PartName} with rarity {rewards[i].Weight}");
                }

                _rewardOptions = rewards;
                return rewards;
            }

            return null;
        }

        /// <summary>
        /// Debug: picks one of the parts the last OnComplete rolled, and returns the other two to the loot pool.
        /// Doesn't equip anything. Temporary until the reward screen exists (task 7).
        /// </summary>
        /// <param name="reward">0, 1, or 2.</param>
        // INTEGRATION: the 1, 2, and 3 debug keys became this Inspector button.
        [Button, DisableInEditorMode]
        private void PickReward(int reward)
        {
            if (_rewardOptions == null || reward >= _rewardOptions.Count)
                return;

            for (int i = 0; i < _rewardOptions.Count; i++)
            {
                if (i == reward)
                {
                    Debug.Log($"{_id} Picked Reward {i + 1}");
                    continue;
                }

                _lootPool.Obtained.Remove(_rewardOptions[i]);
            }
        }
    }
}
