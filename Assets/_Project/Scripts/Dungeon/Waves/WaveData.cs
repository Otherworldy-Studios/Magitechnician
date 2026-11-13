using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "Dungeon/Waves/Wave Data")]
public class WaveData : ScriptableObject
{
    [SerializeField]
    private List<Wave> _waves;
    
    public List<Wave> waves { get { return _waves; } }
    
    public Wave this[int index] { get { return _waves[index]; } set { _waves[index] = value; } }
}
