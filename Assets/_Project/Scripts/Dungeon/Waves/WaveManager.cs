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
    private List<Wave> _waves;
    
    [SerializeField]
    private Room _currentRoom;
    
    [SerializeField] 
    private int _currentWave;

    [SerializeField]
    private int _maxAlive;

    [SerializeField]
    private float minSpawnDistance;

    [Header("Debug (Do not edit)")]
    [SerializeField]
    private List<EnemyBase> _alive;
    
    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCompleted;
    public event Action<EnemyBase> OnBossSpawned;
    public event Action<Room> OnRoomCleared;
    
    override protected void Awake()
    {
        base.Awake();
        
        LevelGenerator.Instance.OnMapGenerated += HandleMapGenerated;
        _currentWave = 0;
        OnWaveCompleted += StartWave;
        GameManager.OnGameLost += StopAll;
    }

    public void HandleMapGenerated()
    {
        foreach (Room room in LevelGenerator.Instance.RoomGrid)
        {
            if (room != null && room.Type != Room.RoomType.Start)
                room.OnRoomEntered += HandlePlayerEntered;
        }
    }

    private void HandlePlayerEntered(Room room)
    {
        SetActiveRoom(room);
        StartCoroutine(RunWave(_currentRoom, 0));
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
        Wave wave = _waves[currentWave];

        yield return new WaitForSeconds(wave.StartDelay);

        while (_alive.Count < _maxAlive && _alive.Count <= wave.EnemiesPerWave)
        {
            SpawnEnemy(wave.Entries[Random.Range(0, wave.Entries.Count)].Enemy, 
                room.Spawnpoints[Random.Range(0, room.Spawnpoints.Count)]);
            
            yield return new WaitForSeconds(wave.SpawnInterval);
        }
    }

    void SpawnEnemy(EnemyBase enemy, Spawnpoint point)
    {
        EnemyBase alive = Instantiate(enemy, point.transform.position, point.transform.rotation);
        alive.Movement.Warp(point.transform.position);
        _alive.Add(alive);
        alive.OnEnemyKilled += HandleEnemyKilled;
    }

    void HandleEnemyKilled(EnemyBase enemy)
    {
        _alive.Remove(enemy);
        
        if (_alive.Count == 0)
        {
            OnWaveCompleted?.Invoke(_currentWave);
            _currentWave++;

            if (_currentWave >= _waves.Count)
            {
                _currentRoom.OnComplete();
                OnRoomCleared?.Invoke(_currentRoom);
            }
        }
    }

    void StartWave(int currentWave)
    {
        if (currentWave < _waves.Count)
        {
            OnWaveStarted?.Invoke(currentWave);
            StartCoroutine(RunWave(_currentRoom, currentWave));
        }
    }

    void SetActiveRoom(Room room)
    {
        _currentRoom = room;
        _currentWave = 0;
    }
}
