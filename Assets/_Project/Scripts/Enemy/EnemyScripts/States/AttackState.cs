using UnityEngine;

/// Summary -
/// Manages the enemy's Attack state.
/// Checks Goldilocks range and Line of Sight (LOS).
/// Attack execution and timing will be integrated separately.
public class AttackState : IEnemyState
{
    private readonly EnemyBase enemy;

    public AttackState(EnemyBase enemy)
    {
        this.enemy = enemy;
    }

    // Stops enemy movement when entering AttackState.
    public void Enter()
    {
        enemy.Movement.StopMoving();

        // TODO: Integrate the team's attack implementation.
    }

    // Checks whether the enemy should leave AttackState.
    public void Update()
    {
        if (!enemy.HasTarget)
        {
            enemy.ChangeState(enemy.IdleState);
            return;
        }

        if (enemy.CurrentRange == RangeStatus.TooClose ||
            !enemy.HasLineOfSight)
        {
            enemy.ChangeState(enemy.RepositionState);
            return;
        }

        if (enemy.CurrentRange == RangeStatus.TooFar)
        {
            enemy.ChangeState(enemy.ChaseState);
            return;
        }
    }

    public void Exit()
    {
    }
}