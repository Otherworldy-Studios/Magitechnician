using UnityEngine;

public class WaveEntry : ScriptableObject
{
    [SerializeField] private EnemyBase _enemy;

    [SerializeField]
    private int count;
    
    public EnemyBase Enemy
    {
        get { return _enemy; }
    }
}
