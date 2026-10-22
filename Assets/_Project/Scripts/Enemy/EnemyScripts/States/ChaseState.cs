using UnityEngine;
/// Summary -
/// Moves the enemy toward its target using EnemyMovement.cs.
/// Uses Goldilocks range and Line of Sight (LOS) to determine whether
/// to chase, reposition, or attack.
public class ChaseState : IEnemyState
{
    private readonly EnemyBase enemy;
    private const float RepathInterval = 0.2f;
    private float nextRepathTime;

    public ChaseState(EnemyBase enemy)
    {
        this.enemy = enemy;
    }
    // Allows immediate path recalculation and resumes movement.
    public void Enter()
    {
        nextRepathTime = 0f;
        enemy.Movement.ResumeMoving();
    }
    // Evaluates target distance and visibility to select the next behavior.
    public void Update()
    {
        if (!enemy.HasTarget)
        {
            enemy.ChangeState(enemy.IdleState);
            return;
        }

        switch (enemy.CurrentRange)
        {
            case RangeStatus.TooClose:
                enemy.ChangeState(enemy.RepositionState);
                return;

            case RangeStatus.InRange:
                if (!enemy.HasLineOfSight)
                {
                    enemy.ChangeState(enemy.RepositionState);
                    return;
                }

                enemy.Movement.StopMoving();
                enemy.ChangeState(enemy.AttackState);

                return;

            case RangeStatus.TooFar:
                if (Time.time >= nextRepathTime)
                {
                    nextRepathTime = Time.time + RepathInterval;
                    enemy.Movement.MoveTo(enemy.Target.position);
                }
                return;
        }
    }

    public void Exit()
    {
    }
}