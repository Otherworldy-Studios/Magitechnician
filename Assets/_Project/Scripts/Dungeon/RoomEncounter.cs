using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Scripts.Dungeon
{
    /// <summary>
    /// Testing stand-in for the wave manager. Put this on a Combat or End room prefab, next to Room.
    ///
    /// Place enemies by hand under one child object (for example "Enemies") and save that child inactive.
    /// When the player enters the room, this turns the child on. When every enemy under it has died,
    /// the room completes, which opens its portal and rolls its rewards.
    ///
    /// The enemies stay off until the room is entered, so they start after the NavMesh is baked
    /// and don't chase the player from other rooms.
    /// </summary>
    [RequireComponent(typeof(Room))]
    public class RoomEncounter : MonoBehaviour
    {
        [Tooltip("Parent of the enemies placed in this room. Save it inactive in the prefab.")]
        [SerializeField] private GameObject _enemiesRoot;

        [Tooltip("Turn on for the End room. Clearing it wins the run.")]
        [SerializeField] private bool _winRunOnClear;

        [ShowInInspector, ReadOnly]
        private readonly List<EnemyBase> _aliveEnemies = new();

        private Room _room;
        private bool _started;

        public int EnemiesLeft => _aliveEnemies.Count;

        [Tooltip("Combat rooms always roll rewards when cleared, and other room types only roll them with this on.")]
        [SerializeField] private bool _rollRewards;

        private void Awake()
        {
            _room = GetComponent<Room>();

            // Backup in case the child was saved active.
            if (_enemiesRoot != null)
                _enemiesRoot.SetActive(false);
        }

        private void OnEnable()
        {
            _room.OnRoomEntered += HandleRoomEntered;
        }

        private void OnDisable()
        {
            _room.OnRoomEntered -= HandleRoomEntered;

            foreach (EnemyBase enemy in _aliveEnemies)
            {
                if (enemy != null)
                    enemy.OnEnemyKilled -= HandleEnemyKilled;
            }
        }

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

        private void HandleEnemyKilled(EnemyBase enemy)
        {
            enemy.OnEnemyKilled -= HandleEnemyKilled;
            _aliveEnemies.Remove(enemy);

            if (_aliveEnemies.Count == 0)
                ClearRoom();
        }

        private void ClearRoom()
        {
            _room.OnComplete(_rollRewards); 

            if (_winRunOnClear && GameManager.HasInstance)
                GameManager.Instance.RequestGameEnd(GameManager.RunResult.Victory);
        }

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