using System;
using System.Collections;
using System.Collections.Generic;
using _Project.Scripts.Dungeon;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class WaveManager : MGR<WaveManager>
{
    [Header("Wave Manager Fields")]
    [SerializeField]
    private List<WaveData> _waves;
    
    [SerializeField]
    private Room _currentRoom;

    [SerializeField]
    private int _maxAlive;

    [SerializeField]
    private float minSpawnDistance;

    [Header("Debug (Do not edit)")]
    [SerializeField] 
    private int _currentWave;
    
    [SerializeField]
    private List<EnemyBase> _alive;

    [SerializeField]
    private bool _spawningDone;
    
    public event Action<int, int> OnWaveStarted;
    public event Action OnWaveCompleted;
    public event Action<EnemyBase> OnBossSpawned;
    public event Action<Room> OnRoomCleared;
    
    override protected void Awake()
    {
        base.Awake();
        
        _currentWave = 0;
        OnWaveCompleted += StartWave;
        GameManager.OnGameLost += StopAll;
    }

    void Start()
    {
        LevelGenerator.Instance.OnMapGenerated += HandleMapGenerated;
        GameManager.OnGameLost += StopAll;
    }

    private void OnDestroy()
    {
        OnWaveCompleted -= StartWave;
        GameManager.OnGameLost -= StopAll;
        LevelGenerator.Instance.OnMapGenerated -= HandleMapGenerated;
        GameManager.OnGameLost -= StopAll;
    }

    public void HandleMapGenerated()
    {
        foreach (Room room in LevelGenerator.Instance.RoomGrid)
        {
            if (room != null && room.Type != Room.RoomType.Start)
            {
                room.OnRoomEntered += HandlePlayerEntered;
                Debug.Log("Attempting to listen for room enter");
            }
        }
    }

    private void HandlePlayerEntered(Room room)
    {
        SetActiveRoom(room);
        Debug.Log($"Entered {room.name}");
        StartCoroutine(RunWave(_currentRoom));
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

        foreach (EnemyBase enemy in _alive)
        {
            enemy.OnEnemyKilled -= HandleEnemyKilled;
            enemy.Health.TakeDamage(instantKill);
            Destroy(enemy.gameObject);
        }
    }

    IEnumerator RunWave(Room room)
    {
        _spawningDone = false;
        Debug.Log($"Spawning wave {_currentWave}");

        if (_currentWave >= _waves.Count)
            yield break;
        
        WaveData waveData = _waves[_currentWave];
        Debug.Log($"Wave data: {waveData.name}");


        yield return new WaitForSeconds(waveData.StartDelay);

        for (int current = 0; current < waveData.Waves.Count; current++)
        {
            foreach (WaveEntry entry in waveData[current].Entries)
            {
                for (int i = 0; i < entry.Count; i++)
                {
                    if (entry.Enemy is PH_Boss)
                        OnBossSpawned?.Invoke(entry.Enemy);

                    SpawnEnemy(entry.Enemy, room.Spawnpoints[Random.Range(0, room.Spawnpoints.Count)]);
                    yield return new WaitForSeconds(waveData[current].SpawnInterval);
                }

                yield return new WaitForSeconds(waveData[current].SpawnInterval);
            }
        }

        _spawningDone = true;
    }

    void SpawnEnemy(EnemyBase enemy, Spawnpoint point)
    {
        if (enemy.IsDead)
            return;
        
        EnemyBase alive = Instantiate(enemy, point.transform.position, point.transform.rotation);
        alive.Movement.Warp(point.transform.position);
        _alive.Add(alive);
        
        alive.OnEnemyKilled += HandleEnemyKilled;
        
        Debug.Log($"Spawned {alive.name}");
    }

    void HandleEnemyKilled(EnemyBase enemy)
    {
        enemy.OnEnemyKilled -= HandleEnemyKilled;
        
        _alive.Remove(enemy);
        Debug.Log($"{enemy.name} has been killed. {_alive.Count} enemies remain");

        if (enemy is PH_Boss) // Replace with actual boss
        {
            GameManager.Instance.RequestGameEnd(GameManager.RunResult.Victory);
            return;
        }

        if (_alive.Count == 0 && _spawningDone)
        {
            Debug.Log($"Wave {_currentWave} has been completed");
            _currentWave++;
            
            OnWaveCompleted?.Invoke();
            
            if (_currentWave >= _waves.Count)
            {
                Debug.Log($"{_currentRoom.Id} has been cleared.");
                
                _currentRoom.OnComplete();
                OnRoomCleared?.Invoke(_currentRoom);
            }
        }
    }

    void StartWave()
    {
        if (_currentWave < _waves.Count)
        {
            OnWaveStarted?.Invoke(_currentWave, _waves.Count);
            Debug.Log($"Started wave {_currentWave}");
            StartCoroutine(RunWave(_currentRoom));
        }
    }

    void SetActiveRoom(Room room)
    {
        _currentRoom = room;
        _currentWave = 0;
    }
}
