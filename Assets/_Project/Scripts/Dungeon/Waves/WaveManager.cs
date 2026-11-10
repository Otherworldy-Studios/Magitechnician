using System;
using System.Collections.Generic;
using _Project.Scripts.Dungeon;
using UnityEngine;

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

    private event Action<int, int> OnWaveStarted;
    private event Action<int> OnEnemiesRemaningChanged;
    private event Action<int> OnWaveCompleted;
    private event Action<Room> OnBossSpawned;
    
    override protected void Awake()
    {
        base.Awake();
        LevelGenerator.Instance.OnMapGenerated += SubscribeToRoomEvents;
        _currentWave = 0;
    }

    void SubscribeToRoomEvents()
    {

    }

    void SetActiveRoom(Room room)
    {
        _currentRoom = room;
    }
}
