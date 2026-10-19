using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStats", menuName = "Scriptable Objects/EnemyStats")]
public class EnemyStats : ScriptableObject
{
    [SerializeField] private float _detectionRange = 10f;
    [SerializeField] private float _goldilocksRangeMin = 0f;
    [SerializeField] private float _goldilocksRangeMax = 1f;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _attackWindUpTime = 2f;
    [SerializeField] private float _attackCooldownTime = 3f;
    [SerializeField] private float _health = 100f;
}
