using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Scripts.Dungeon
{
    /// <summary>
    /// Testing stand-in for the wave manager. Put this on a room prefab, next to Room.
    ///
    /// Place enemies by hand under one child object (for example "Enemies") and save that child inactive.
    /// When the player enters the room, this turns the child on. When every enemy under it has died,
    /// the room completes, which opens its portal and rolls its rewards.
    ///
    /// The enemies stay off until the room is entered, so they start after the NavMesh is baked
    /// and don't chase the player from other rooms.
    /// </summary>
    // INTEGRATION: new. Listens to Room.OnRoomEntered (also new) and to EnemyBase.OnEnemyKilled, then calls
    // Room.OnComplete, which opens the Portal. The wave manager (Sprint 3) replaces this.
    [RequireComponent(typeof(Room))]
    public class RoomEncounter : MonoBehaviour
    {
        [Tooltip("Parent of the enemies placed in this room. Save it inactive in the prefab.")]
        [SerializeField] private GameObject _enemiesRoot;

        [Tooltip("Turn on for the End room. Clearing it wins the run.")]
        [SerializeField] private bool _winRunOnClear;

        [Tooltip("Combat rooms always roll rewards when cleared, and other room types only roll them with this on.")]
        [SerializeField] private bool _rollRewards; // Passed to Room.OnComplete as force.

        // Enemies still alive in this room. Each one is removed when its OnEnemyKilled fires.
        [ShowInInspector, ReadOnly]
        private readonly List<EnemyBase> _aliveEnemies = new();

        private Room _room;    // The Room on the same object.
        private bool _started; // True once the enemies were turned on, so entering again doesn't restart them.

        /// <summary>How many enemies are still alive in this room.</summary>
        public int EnemiesLeft => _aliveEnemies.Count;

        /// <summary>Finds the Room and makes sure the enemies start off.</summary>
        private void Awake()
        {
            _room = GetComponent<Room>();

            // Backup in case the child was saved active.
            if (_enemiesRoot != null)
                _enemiesRoot.SetActive(false);
        }

        /// <summary>Starts listening for the player entering the room.</summary>
        private void OnEnable()
        {
            _room.OnRoomEntered += HandleRoomEntered;
        }

        /// <summary>Stops listening to the room and to every enemy still alive.</summary>
        private void OnDisable()
        {
            _room.OnRoomEntered -= HandleRoomEntered;

            foreach (EnemyBase enemy in _aliveEnemies)
            {
                if (enemy != null)
                    enemy.OnEnemyKilled -= HandleEnemyKilled;
            }
        }

        /// <summary>
        /// Runs when Room.EnterRoom fires OnRoomEntered. The first time, turns on the enemies and starts tracking them.
        /// A room with no living enemies clears at once.
        /// </summary>
        private void HandleRoomEntered(Room room)
        {
            if (_started || _room.Complete)
                return;

            _started = true;

            if (_enemiesRoot != null)
            {
                _enemiesRoot.SetActive(true);

                foreach (EnemyBase enemy in _enemiesRoot.GetComponentsInChildren<EnemyBase>())
                {
                    if (enemy.IsDead) continue;

                    _aliveEnemies.Add(enemy);
                    enemy.OnEnemyKilled += HandleEnemyKilled;
                }
            }

            // A room with no enemies clears as soon as it's entered.
            if (_aliveEnemies.Count == 0)
                ClearRoom();
        }

        /// <summary>Runs when one of this room's enemies dies. Clears the room once none are left.</summary>
        private void HandleEnemyKilled(EnemyBase enemy)
        {
            enemy.OnEnemyKilled -= HandleEnemyKilled;
            _aliveEnemies.Remove(enemy);

            if (_aliveEnemies.Count == 0)
                ClearRoom();
        }

        /// <summary>
        /// Completes the room through Room.OnComplete, which opens the portal and rolls rewards.
        /// In the End room, also ends the run as a win through GameManager.RequestGameEnd.
        /// </summary>
        private void ClearRoom()
        {
            _room.OnComplete(_rollRewards);

            if (_winRunOnClear && GameManager.HasInstance)
                GameManager.Instance.RequestGameEnd(GameManager.RunResult.Victory);
        }

        /// <summary>Debug: kills every enemy still alive in this room.</summary>
        [Button, DisableInEditorMode]
        private void DEBUGKillAll()
        {
            foreach (EnemyBase enemy in _aliveEnemies.ToArray())
            {
                if (enemy != null)
                    enemy.Die();
            }
        }
    }
}
