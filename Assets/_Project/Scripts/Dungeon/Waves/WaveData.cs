using System.Collections.Generic;
using UnityEngine;

public class WaveData : ScriptableObject
{
    [SerializeField] 
    private List<EnemyBase> _enemies;

    [SerializeField]
    [Range(3, 6)]
    private int _enemiesPerWave;

    [SerializeField]
    private int _numWaves;
    
    public List<EnemyBase> Enemies => _enemies;
    public int EnemiesPerWave => _enemiesPerWave;
    public int NumWaves => _numWaves;
}
