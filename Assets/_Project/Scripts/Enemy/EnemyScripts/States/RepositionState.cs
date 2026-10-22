
using UnityEngine;
using UnityEngine.AI;

/// Summary -
/// Moves the enemy away from the player when too close.
/// Uses the Goldilocks range and NavMesh to find a
/// preferred position, then returns to ChaseState.
public class RepositionState : IEnemyState
{
    private readonly EnemyBase enemy;

    private const float SampleRadius = 2f;
    private const float RepathInterval = 0.25f;

    private float nextRepathTime;

    public RepositionState(EnemyBase enemy)
    {
        this.enemy = enemy;
    }
    // Allows immediate position recalculation and stops current movement.
    public void Enter()
    {
        nextRepathTime = 0f;
        enemy.Movement.StopMoving();
    }
    // Checks the preferred range and calculates a new NavMesh destination.
    public void Update()
    {
        if (!enemy.HasTarget)
        {
            enemy.ChangeState(enemy.IdleState);
            return;
        }

        RangeStatus status = enemy.CurrentRange;

        if (status == RangeStatus.TooFar)
        {
            enemy.ChangeState(enemy.ChaseState);
            return;
        }

        if (status == RangeStatus.InRange &&
            enemy.HasLineOfSight)
        {
            enemy.ChangeState(enemy.ChaseState);
            return;
        }

        if (enemy.Range == null)
        {
            enemy.Movement.StopMoving();
            return;
        }

        if (Time.time < nextRepathTime)
            return;

        nextRepathTime = Time.time + RepathInterval;

        Vector3 away =
            enemy.transform.position - enemy.Target.position;

        away.y = 0f;

        if (away.sqrMagnitude < 0.001f)
            away = -enemy.transform.forward;

        away.Normalize();

        float preferredDistance =
            (enemy.Range.MinDistance +
             enemy.Range.MaxDistance) * 0.5f;

        Vector3 desiredPosition =
            enemy.Target.position +
            away * preferredDistance;

        if (NavMesh.SamplePosition(
                desiredPosition,
                out NavMeshHit hit,
                SampleRadius,
                NavMesh.AllAreas))
        {
            enemy.Movement.MoveTo(hit.position);
        }
        else
        {
            enemy.Movement.StopMoving();
        }
    }

    public void Exit()
    {
    }
}