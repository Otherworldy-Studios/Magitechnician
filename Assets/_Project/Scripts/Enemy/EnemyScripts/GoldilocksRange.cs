using UnityEngine;

/// <summary>
/// Tells whether the enemy is too close to, too far from, or within its preferred distance of the target,
/// using the MinRange and MaxRange stats. EnemyBase.CurrentRange reads this through IEnemyRange.
/// </summary>
[RequireComponent(typeof(EnemyBase))]
public class GoldilocksRange : MonoBehaviour, IEnemyRange
{
    // INTEGRATION: removed the EnemyStats field. The distances now come from EnemyBase, which reads them from Stats.
    private EnemyBase _enemy;

    /// <summary>Closest preferred distance.</summary>
    public float MinDistance => _enemy.MinRange;

    /// <summary>Farthest preferred distance.</summary>
    public float MaxDistance => _enemy.MaxRange;

    /// <summary>Finds the EnemyBase.</summary>
    private void Awake()
    {
        _enemy = GetComponent<EnemyBase>();
    }

    /// <summary>Compares the distance to the target with MinDistance and MaxDistance. TooFar when there's no target.</summary>
    public RangeStatus GetRangeStatus()
    {
        if (_enemy.Target == null)
            return RangeStatus.TooFar;

        float distance = Vector3.Distance(
            transform.position,
            _enemy.Target.position
        );

        if (distance < MinDistance)
            return RangeStatus.TooClose;

        if (distance > MaxDistance)
            return RangeStatus.TooFar;

        return RangeStatus.InRange;
    }
}
