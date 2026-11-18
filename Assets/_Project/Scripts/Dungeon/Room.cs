using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = System.Random;

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

        [Tooltip("Where the player arrives. Its blue (Z) axis is the way the player faces after a teleport.")]
        [SerializeField]
        private GameObject _playerSpawn;

        [Tooltip("The room's size. The generator spaces rooms by its diagonal.")]
        [SerializeField]
        private BoxCollider _boundingBox;

        public BoxCollider BoundingBox { get => _boundingBox; }
        
        [Header("Chest")]
        [SerializeField] private GameObject _chest;

        [Tooltip("Chance to spawn in consumable chest in room.")]
        [SerializeField, Range(0f,1f)] private float chestChance = 0.5f;

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
        public static event Action<Room> OnRoomCleared; //made static so that listeners dont need to subscribe to individual isntances of rooms

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

        /// <summary>Finds the exit portal and tells it which room it belongs to.</summary>
        private void Awake()
        {
            _portal = GetComponentInChildren<Portal>();

            if (_portal == null)
                Debug.Log($"{name} has no exit portal"); // INTEGRATION: was _id, which the generator hasn't set yet in Awake.
            else
                _portal.Parent = this;

            _spawnpoints = GetComponentsInChildren<Spawnpoint>().ToList()
                .Where(x => x.CanSpawnEnemies).ToList();
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
        public void OnComplete(bool force = false)
        {
            
            if (_complete)
            {
                return; //changed the way it prevents the double trigger so that the UI can listen to the roomcleared event better
            }
            
            _complete = true;
            
            if (_portal != null && _portal.Connected != null)
                _portal.Active = true;
            
            OnRoomCleared?.Invoke(this);
        }
        
        
        public void RollChest()
        {
            if (UnityEngine.Random.value < chestChance)
            {
                SpawnChest();
            }
            else
            {
                Debug.Log($"player failed chest roll");
            }
        }
        
        /// <summary>Button will simply spawn in the chest guaranteed.</summary>
        [Button, DisableInEditorMode]
        public void SpawnChest()
        {
            if (_chest == null)
            {
                Debug.LogWarning($"{name} has no chest assigned.");
                return;
            }

            _chest.SetActive(true);
        }
        
    }
}
