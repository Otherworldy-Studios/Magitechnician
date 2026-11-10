using System.Collections.Generic;
using UnityEngine;

public class Wave : ScriptableObject
{
    private List<WaveEntry> _entries;

    [SerializeField]
    private int _enemiesPerWave;

    [SerializeField]
    private float _startDelay;

    [SerializeField]
    private float _spawnInterval;
    
    public List<WaveEntry> Entries { get { return _entries; } }
    public int EnemiesPerWave => _enemiesPerWave;
    public float StartDelay => _startDelay;
    public float SpawnInterval => _spawnInterval;
}