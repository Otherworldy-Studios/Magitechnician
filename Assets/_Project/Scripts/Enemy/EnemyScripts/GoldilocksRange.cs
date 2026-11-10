using UnityEngine;

/// <summary>
/// Determines whether the enemy is too close, far, or within
/// its ideal range of the player using the MinRange and MaxRange stats.
/// </summary>

[RequireComponent(typeof(EnemyBase))]
public class GoldilocksRange : MonoBehaviour, IEnemyRange
{
  

    private EnemyBase enemy;

    public float MinDistance => enemy.MinRange;
    public float MaxDistance => enemy.MaxRange; 

    private void Awake()
    {
        enemy = GetComponent<EnemyBase>();
    }

    public RangeStatus GetRangeStatus()
    {
        if (enemy.Target == null)
            return RangeStatus.TooFar;

        float distance = Vector3.Distance(
            transform.position,
            enemy.Target.position
        );

        if (distance < MinDistance)
            return RangeStatus.TooClose;

        if (distance > MaxDistance)
            return RangeStatus.TooFar;

        return RangeStatus.InRange;
    }
}