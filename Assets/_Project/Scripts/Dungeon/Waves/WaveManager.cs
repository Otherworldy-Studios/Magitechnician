using _Project.Scripts.Dungeon;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class WaveManager : MGR<WaveManager>
{
    [Header("Wave Manager Fields")]
    [SerializeField] private Room _currentRoom;
    [Tooltip("Most enemies alive at once. Spawning waits while the limit is reached. 0 means no limit.")]
    [SerializeField] private int _maxAlive;
    [Tooltip("Enemies spawn at least this far from the player on the ground plane, when a spawn point allows it.")]
    [SerializeField] private float _minSpawnDistance;
    [Tooltip("How much stronger enemies get in each room. Leave empty for no scaling.")]
    [SerializeField] private DifficultyScaling _difficultyScaling;
    [SerializeField] GameObject _enemySpawnVFX;
    [Tooltip("Small Delay Before Enemy Spawns to play VFX.")]
    [SerializeField] private float _spawnDelay = 0.75f;

    [Header("Debug (Do not edit)")]
    [SerializeField] private int _currentWave;
    [SerializeField] private List<EnemyBase> _alive;
    [SerializeField] private bool _spawningDone;
    [SerializeField] private bool _waveRunning;
  

    public event Action<int, int> OnWaveStarted;
    public event Action OnWaveCompleted;
    public event Action<EnemyBase> OnBossSpawned;
    public event Action<EnemyBase> OnEnemySpawned;
    public event Action<EnemyBase> OnEnemyKilled;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }

        _currentWave = 0;
        OnWaveCompleted += StartWave;
    }

    private void OnEnable()
    {
        LevelGenerator.Instance.OnMapGenerated += HandleMapGenerated;
    }

    private void OnDisable()
    {
        if (LevelGenerator.HasInstance)
        {
            LevelGenerator.Instance.OnMapGenerated -= HandleMapGenerated;
        }
    }

    private void Start()
    {
        if (LevelGenerator.Instance.GenerationComplete)
        {
            HandleMapGenerated();
        }

        GameManager.OnGameLost += StopAll;
    }

    private void OnDestroy()
    {
        OnWaveCompleted -= StartWave;
        GameManager.OnGameLost -= StopAll;
    }

    public void HandleMapGenerated()
    {
        foreach (Room room in LevelGenerator.Instance.RoomGrid)
        {
            if (room != null)
            {
                room.OnRoomEntered -= HandlePlayerEntered;
                room.OnRoomEntered += HandlePlayerEntered;
            }
        }

        // The End room isn't in the grid.
        Room endRoom = LevelGenerator.Instance.EndRoom;
        if (endRoom != null)
        {
            endRoom.OnRoomEntered -= HandlePlayerEntered;
            endRoom.OnRoomEntered += HandlePlayerEntered;
        }
    }

    private void HandlePlayerEntered(Room room)
    {
        if (_waveRunning)
            return;

        SetActiveRoom(room);

        _waveRunning = true;

        StartCoroutine(RunWave(_currentRoom));
        room.OnRoomEntered -= HandlePlayerEntered;
    }

    public void StopAll()
    {
        StopAllCoroutines();
        KillAll();
    }

    [Button, DisableInEditorMode]
    public void KillAll()
    {
        DamageInfo instantKill = new DamageInfo()
        {
            DamageAmount = 2_147_483_647,
            DamageType = DamageType.Physical,
            DamageDirection = Vector3.zero,
            DamageSource = null
        };

        List<EnemyBase> aliveCopy = new List<EnemyBase>(_alive);

        foreach (EnemyBase enemy in aliveCopy)
        {
            enemy.OnEnemyKilled -= HandleEnemyKilled;
            enemy.Health.TakeDamage(instantKill);
            Destroy(enemy.gameObject);
        }

        _alive.Clear();
        _waveRunning = false;
        _spawningDone = false;
    }

    /// <summary>
    /// Spawns the room's current Wave, one enemy per spawn interval, at random spawn points.
    /// Sets _spawningDone when every enemy in the Wave has spawned, then ends the Wave if those enemies are already dead.
    /// </summary>
    private IEnumerator RunWave(Room room)
    {
        if (room != _currentRoom || !_waveRunning)
            yield break;

        if (room.Spawnpoints.Count == 0)
        {
            Debug.LogWarning($"{room.gameObject.name} doesn't have any spawn points assigned.");
            room.OnComplete();
            _waveRunning = false;
            yield break;
        }

        if (!room.RoomWaves)
        {
            Debug.LogWarning($"{room.gameObject.name} has no RoomWaves assigned.");
            room.OnComplete();
            _waveRunning = false;
            yield break;
        }

        _spawningDone = false;

        if (_currentWave >= room.RoomWaves.Waves.Count)
        {
            Debug.LogWarning($"{room.gameObject.name} has no wave at index {_currentWave}.");
            room.OnComplete();
            _waveRunning = false;
            yield break;
        }

        OnWaveStarted?.Invoke(_currentWave, room.RoomWaves.Waves.Count);

        WaveData waveData = room.RoomWaves;

        yield return new WaitForSeconds(waveData.StartDelay);

        foreach (WaveEntry entry in waveData[_currentWave].Entries)
        {
            for (int i = 0; i < entry.Count; i++)
            {
                yield return new WaitUntil(() => _maxAlive <= 0 || _alive.Count < _maxAlive);
                Spawnpoint sp = ChooseSpawnpoint(room.Spawnpoints[Random.Range(0, room.Spawnpoints.Count)]);
                GameObject effect = GameObject.Instantiate(_enemySpawnVFX, sp.transform.position, Quaternion.identity);
                SoundController.Instance.PlaySFX("Enemy Spawn Sound", sp.transform.position);
                yield return new WaitForSeconds(_spawnDelay);
                SpawnEnemy(entry.Enemy,sp);
                Destroy(effect, 0.25f);
                yield return new WaitForSeconds(waveData[_currentWave].SpawnInterval);
            }
        }

        _spawningDone = true;

        // The player can kill every enemy during the last spawn interval, before _spawningDone was set.
        TryFinishWave();
    }

    private void SpawnEnemy(EnemyBase enemy, Spawnpoint spawnpoint)
    {
        //Spawnpoint spawnpoint = ChooseSpawnpoint(point);
      
        
        EnemyBase alive = Instantiate(enemy, spawnpoint.transform.position, spawnpoint.transform.rotation);
        alive.Movement.Warp(spawnpoint.transform.position);
        ApplyDifficulty(alive);

        if (alive is StoneGuardian)
        {
            OnBossSpawned?.Invoke(alive);
        }
     

        _alive.Add(alive);
        alive.MakeAggressive();
        alive.OnEnemyKilled += HandleEnemyKilled;
        OnEnemySpawned?.Invoke(alive);
    }

    /// <summary>Adds the difficulty tier's modifiers for the rooms cleared so far to the enemy's Stats.</summary>
    private void ApplyDifficulty(EnemyBase enemy)
    {
        if (_difficultyScaling == null || enemy.Stats == null)
            return;

        int roomsCleared = GameManager.HasInstance ? GameManager.Instance.RunStats.RoomsCleared : 0;

        foreach (StatModifier modifier in _difficultyScaling.GetModifiers(roomsCleared))
        {
            enemy.Stats.AddModifier(modifier, this);
        }
    }

    /// <summary>
    /// Returns the given spawn point if it's at least _minSpawnDistance from the player on the ground plane.
    /// Otherwise returns a random spawn point in the room that is, or the farthest one if none are.
    /// </summary>
    private Spawnpoint ChooseSpawnpoint(Spawnpoint preferred)
    {
        GameObject player = GameManager.HasInstance ? GameManager.Instance.Player : null;

        if (player == null || _minSpawnDistance <= 0f)
            return preferred;

        // The PlayerRoot stays where the prefab was placed. The CharacterController is the part that moves.
        CharacterController controller = player.GetComponentInChildren<CharacterController>();
        Vector3 playerPosition = controller != null ? controller.transform.position : player.transform.position;

        if (GroundDistance(preferred.transform.position, playerPosition) >= _minSpawnDistance)
            return preferred;

        List<Spawnpoint> farEnough = new List<Spawnpoint>();
        Spawnpoint farthest = preferred;
        float farthestDistance = GroundDistance(preferred.transform.position, playerPosition);

        foreach (Spawnpoint candidate in _currentRoom.Spawnpoints)
        {
            float distance = GroundDistance(candidate.transform.position, playerPosition);

            if (distance >= _minSpawnDistance)
                farEnough.Add(candidate);

            if (distance > farthestDistance)
            {
                farthest = candidate;
                farthestDistance = distance;
            }
        }

        return farEnough.Count > 0 ? farEnough[Random.Range(0, farEnough.Count)] : farthest;
    }

    private static float GroundDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    /// <summary>Removes the enemy from the alive list, and ends the run when the boss dies. Otherwise checks whether the Wave is over.</summary>
    private void HandleEnemyKilled(EnemyBase enemy)
    {
        enemy.OnEnemyKilled -= HandleEnemyKilled;

        _alive.Remove(enemy);
        OnEnemyKilled?.Invoke(enemy);

        if (enemy is StoneGuardian)
        {
            GameManager.Instance.RequestGameEnd(GameManager.RunResult.Victory);
            return;
        }

        TryFinishWave();
    }

    /// <summary>
    /// When every enemy in the Wave has spawned and died, moves to the next Wave, or completes the room after the last
    /// Wave. Runs after each kill and once spawning finishes.
    /// </summary>
    private void TryFinishWave()
    {
        if (_alive.Count > 0 || !_spawningDone)
            return;

        _currentWave++;

        OnWaveCompleted?.Invoke();

        if (_currentWave >= _currentRoom.RoomWaves.Waves.Count)
        {
            _currentRoom.OnComplete();
            _waveRunning = false;
        }
    }

    private void StartWave()
    {
        if (_currentRoom.Complete || !_waveRunning)
            return;

        if (_currentWave < _currentRoom.RoomWaves.Waves.Count)
        {
            StartCoroutine(RunWave(_currentRoom));
        }
    }

    private void SetActiveRoom(Room room)
    {
        _currentRoom = room;
        _currentWave = 0;
    }
}