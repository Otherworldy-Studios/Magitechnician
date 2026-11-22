using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Scripts.Dungeon
{
    /// <summary>
    /// One room of the dungeon. Goes on the root of a room prefab. Holds the room's type, waves, player spawn point,
    /// bounding box, chest, exit portal, and links to its neighbors. EnterRoom runs when the player arrives, and
    /// OnComplete when the room is cleared, which rolls the chest, opens the portal, and fires OnRoomCleared. When the
    /// chest appears, the portal waits until the chest is opened.
    /// </summary>
    public class Room : MonoBehaviour
    {
        public enum RoomType
        {
            Start,
            Combat,
            End
        }

        [Header("Room Settings")]
        [SerializeField] private RoomType _roomType;

        [Tooltip("Where the player arrives. Its blue (Z) axis is the way the player faces after a teleport.")]
        [SerializeField] private GameObject _playerSpawn;

        [Tooltip("The room's size. The generator spaces rooms by its diagonal.")]
        [SerializeField] private BoxCollider _boundingBox;

        [Tooltip("The Waves WaveManager spawns in this room.")]
        [SerializeField] private WaveData _roomWaves;

        [Header("Chest")]
        [Tooltip("The room's chest, turned off in the prefab. RollChest turns it on. Needs a TreasureChest on it or a child.")]
        [SerializeField] private GameObject _chest;

        [Tooltip("Chance that the chest appears after the reward is picked, from 0 to 1.")]
        [SerializeField, Range(0f, 1f)] private float _chestChance = 0.5f;

        #region DebugFields
        [Header("Debug (Do not edit)"), Space(5)]
        [Tooltip("Set by the generator, such as \"Combat2\".")]
        [SerializeField, ReadOnly] private string _id;
        [Tooltip("True once OnComplete has run.")]
        [SerializeField, ReadOnly] private bool _complete;
        [SerializeField, ReadOnly] private List<Spawnpoint> _spawnpoints;
        [Tooltip("The exit. Found in children in Awake.")]
        [SerializeField, ReadOnly] private Portal _portal;
        [Tooltip("The TreasureChest on Chest. Found in Awake.")]
        [SerializeField, ReadOnly] private TreasureChest _treasureChest;

        [Tooltip("Neighbor link, set by the generator. Null where there's no room.")]
        [SerializeField, ReadOnly] private Room _north;
        [Tooltip("Neighbor link, set by the generator. Null where there's no room.")]
        [SerializeField, ReadOnly] private Room _south;
        [Tooltip("Neighbor link, set by the generator. Null where there's no room.")]
        [SerializeField, ReadOnly] private Room _east;
        [Tooltip("Neighbor link, set by the generator. Null where there's no room.")]
        [SerializeField, ReadOnly] private Room _west;
        #endregion

        public event Action<Room> OnRoomEntered;
        public static event Action<Room> OnRoomCleared;

        public RoomType Type => _roomType;
        public WaveData RoomWaves => _roomWaves;
        public BoxCollider BoundingBox => _boundingBox;
        public Vector3 PlayerSpawnPos => _playerSpawn.transform.position;
        public Quaternion PlayerSpawnRotation => _playerSpawn.transform.rotation;
        public IReadOnlyList<Spawnpoint> Spawnpoints => _spawnpoints;
        public Portal Portal => _portal;
        public bool Complete => _complete;

        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public Room North
        {
            get => _north;
            set => _north = value;
        }

        public Room South
        {
            get => _south;
            set => _south = value;
        }

        public Room East
        {
            get => _east;
            set => _east = value;
        }

        public Room West
        {
            get => _west;
            set => _west = value;
        }

        private void Awake()
        {
            _portal = GetComponentInChildren<Portal>();

            if (_portal != null)
                _portal.Parent = this;

            _spawnpoints = GetComponentsInChildren<Spawnpoint>().ToList().Where(x => x.CanSpawnEnemies).ToList();

            if (_chest != null)
                _treasureChest = _chest.GetComponentInChildren<TreasureChest>(true);

            if (_treasureChest != null)
                _treasureChest.OnOpened += OpenPortal;
        }

        private void OnDestroy()
        {
            if (_treasureChest != null)
                _treasureChest.OnOpened -= OpenPortal;
        }

        /// <summary>
        /// Marks this as the player's current room and fires OnRoomEntered. Portal calls this after moving the player in,
        /// and GameManager.StartRun calls it for the start room.
        /// </summary>
        public void EnterRoom()
        {
            if (GameManager.HasInstance)
                GameManager.Instance.ChangeRoom(this);

            OnRoomEntered?.Invoke(this);
        }

        /// <summary>
        /// Clears the room the first time: rolls the chest, opens the portal unless a chest appeared, and fires
        /// OnRoomCleared. With a chest, the portal opens when the chest is opened.
        /// Called by WaveManager when the last Wave is cleared, by LevelGenerator's debug button for the start room, and as
        /// a debug button.
        /// </summary>
        /// <param name="force">Not used.</param>
        [Button, DisableInEditorMode]
        public void OnComplete(bool force = false)
        {
            if (_complete)
                return;

            _complete = true;

            if (!RollChest())
                OpenPortal();

            OnRoomCleared?.Invoke(this);
        }

        /// <summary>
        /// Turns on the chest, with a _chestChance chance. Returns true if a chest appeared that the player has to open.
        /// A chest without a TreasureChest counts as no chest, so the portal still opens.
        /// </summary>
        private bool RollChest()
        {
            if (_chest == null || UnityEngine.Random.value >= _chestChance)
                return false;

            SpawnChest();
            return _treasureChest != null;
        }

        /// <summary>Opens the exit portal if it leads somewhere. Runs from OnComplete, or when the chest is opened.</summary>
        private void OpenPortal()
        {
            if (_portal != null && _portal.Connected != null)
            {
                _portal.Active = true;
                SoundController.Instance.PlaySFX("Portal Opens", _portal.transform.position);
            }
               
        }

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