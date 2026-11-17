using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "Dungeon/Waves/Wave Entry")]
public class WaveEntry : ScriptableObject
{
    [SerializeField] private EnemyBase _enemy;

    [SerializeField]
    private int count;
    
    public EnemyBase Enemy
    {
        get { return _enemy; }
    }
    
    public int Count { get { return count; } }
}
