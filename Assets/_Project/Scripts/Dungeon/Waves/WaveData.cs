using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "Dungeon/Waves/Wave Data")]
public class WaveData : SerializedScriptableObject
{
    [OdinSerialize]
    private List<Wave> _waves;

    [OdinSerialize]
    private float _startDelay;

    public float StartDelay => _startDelay;
    
    public List<Wave> Waves { get { return _waves; } }
    
    public Wave this[int index] { get { return _waves[index]; } }
}
