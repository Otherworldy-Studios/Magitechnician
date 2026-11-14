using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "Dungeon/Waves/Enemy Wave")]
public class Wave : ScriptableObject
{
    [SerializeField]
    private List<WaveEntry> _entries;

    [SerializeField]
    private float _spawnInterval;
    
    public List<WaveEntry> Entries { get { return _entries; } }
    public float SpawnInterval => _spawnInterval;
}