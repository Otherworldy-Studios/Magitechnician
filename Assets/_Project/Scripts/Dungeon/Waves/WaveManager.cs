using System;
using System.Collections;
using System.Collections.Generic;
using _Project.Scripts.Dungeon;
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
    
    public event Action<int> OnWaveStarted;
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
    }
    
    public void HandleMapGenerated()
    {
        foreach (Room room in LevelGenerator.Instance.RoomGrid)
        {
            if (room != null)
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
        StartCoroutine(RunWave(_currentRoom, _currentWave));
    }

    public void StopAll()
    {
        StopAllCoroutines();
        KillAll();
    }

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
            enemy.Health.TakeDamage(instantKill);
    }

    IEnumerator RunWave(Room room, int currentWave)
    {
        _spawningDone = false;
        WaveData wave = _waves[currentWave];
        
        yield return new WaitForSeconds(wave[currentWave].StartDelay);

        foreach (WaveEntry entry in wave.waves[currentWave].Entries)
        {
            for (int i = 0; i < entry.Count; i++)
            {
                SpawnEnemy(entry.Enemy, room.Spawnpoints[Random.Range(0, room.Spawnpoints.Count)]);

                yield return new WaitForSeconds(wave[currentWave].SpawnInterval);
            }
            
            yield return new WaitForSeconds(wave[currentWave].SpawnInterval);
        }

        _spawningDone = true;
    }

    void SpawnEnemy(EnemyBase enemy, Spawnpoint point)
    {
        EnemyBase alive = Instantiate(enemy, point.transform.position, point.transform.rotation);
        alive.Movement.Warp(point.transform.position);
        _alive.Add(alive);
        
        alive.OnEnemyKilled += HandleEnemyKilled;
        
        Debug.Log($"Spawned {alive.name}");
    }

    void HandleEnemyKilled(EnemyBase enemy)
    {
        _alive.Remove(enemy);
        Debug.Log($"{enemy.name} has been killed. {_alive.Count} enemies remain");
        
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
            OnWaveStarted?.Invoke(_currentWave);
            Debug.Log($"Started wave {_currentWave}");
            StartCoroutine(RunWave(_currentRoom, _currentWave));
        }
    }

    void SetActiveRoom(Room room)
    {
        _currentRoom = room;
        _currentWave = 0;
    }
}
