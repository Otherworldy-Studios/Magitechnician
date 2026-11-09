using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "WaveData")]
public class WaveData : ScriptableObject
{
    public List<GameObject> enemyPrefabs; //allows any enemy type
    
    [SerializeField, Range(1, 15)] //15 is probably an absurd amount but just in case yknow
    public int enemiesPerWave;

    [SerializeField, Min(1)]
    public int waveAmount;

}
