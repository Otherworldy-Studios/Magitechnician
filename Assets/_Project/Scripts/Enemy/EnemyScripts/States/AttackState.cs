using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Manages enemy attack behavior.
/// Checks goldilocks range and LOS, faces the player,
/// and starts attack when their cooldown is ready.
/// </summary>
public class AttackState : IEnemyState
{
    private const float RotationSpeed = 360f;
    private readonly EnemyBase enemy;
    private NavMeshAgent agent;
    private EnemyAttack currentAttack;
    private bool previousUpdateRotation;

    public AttackState(EnemyBase enemy)
    {
        this.enemy = enemy;
        agent = enemy.GetComponent<NavMeshAgent>();
    }

    // Stops enemy movement when entering AttackState.
    public void Enter()
    {
        enemy.Movement.StopMoving();

        currentAttack = null;

        foreach (EnemyAttack attack in enemy.Attacks)
        {
            if (attack != null && attack.isActiveAndEnabled)
            {
                currentAttack = attack;
                break;
            }
        }

        // AttackState controls rotation while attacking.
        if (agent != null)
        {
            previousUpdateRotation = agent.updateRotation;
            agent.updateRotation = false;
        }
    }

    // Checks whether the enemy should leave AttackState.
    public void Tick(float dt)
    {
        if (!enemy.HasTarget)
        {
            enemy.ChangeState(enemy.IdleState);
            return;
        }

        RangeStatus rangeStatus = enemy.CurrentRange;

        if (rangeStatus == RangeStatus.TooClose || !enemy.HasLineOfSight)
        {
            enemy.ChangeState(enemy.RepositionState);
            return;
        }

        if (rangeStatus == RangeStatus.TooFar)
        {
            enemy.ChangeState(enemy.ChaseState);
            return;
        }
        
        // Face the player while attacking.
        Vector3 direction = enemy.Target.position - enemy.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            enemy.transform.rotation = Quaternion.RotateTowards(
                enemy.transform.rotation,
                Quaternion.LookRotation(direction),
                RotationSpeed * dt
            );
        }

        if (currentAttack != null && currentAttack.CanAttack)
            currentAttack.StartAttack();
    }

    public void Exit()
    {
        // Stops an unfinished attack when leaving AttackState
        if (currentAttack != null)
            currentAttack.CancelAttack();

        // Restore the NavMeshAgent's original rotation setting
        if  (agent != null)
            agent.updateRotation = previousUpdateRotation;

        currentAttack = null;
    }
}