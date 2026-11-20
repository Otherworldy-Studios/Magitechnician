using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Manages enemy attack behavior.
/// Checks goldilocks range and LOS, faces the player,
/// and starts attack when their cooldown is ready.
/// </summary>
public class AttackState : IEnemyState
{
    private const float RotationSpeed = 360f; // Degrees per second the enemy turns to face the target.
    private readonly EnemyBase _enemy;
    private NavMeshAgent _agent;          // Its rotation is turned off while attacking, so this state can face the target.
    private EnemyAttack _currentAttack;   // The first active attack, picked in Enter.
    private bool _previousUpdateRotation; // The agent's rotation setting before Enter, put back in Exit.

    /// <summary>Creates the state for one enemy. EnemyBase.Awake calls this.</summary>
    public AttackState(EnemyBase enemy)
    {
        _enemy = enemy;
        _agent = _enemy.GetComponent<NavMeshAgent>();
    }

    /// <summary>Stops moving, picks the first active attack, and takes over rotation from the NavMeshAgent.</summary>
    public void Enter()
    {
        _enemy.Movement.StopMoving();

        _currentAttack = null;

        foreach (EnemyAttack attack in _enemy.Attacks)
        {
            if (attack != null && attack.isActiveAndEnabled)
            {
                _currentAttack = attack;
                break;
            }
        }

        // AttackState controls rotation while attacking.
        if (_agent != null)
        {
            _previousUpdateRotation = _agent.updateRotation;
            _agent.updateRotation = false;
        }
    }

    /// <summary>
    /// Leaves for Idle, Reposition, or Chase when the target is lost, too close, out of sight, or too far.
    /// Otherwise turns toward the target and starts the attack when its cooldown is ready.
    /// </summary>
    public void Tick(float dt)
    {
        if (!_enemy.HasTarget)
        {
            _enemy.ChangeState(_enemy.IdleState);
            return;
        }

        RangeStatus rangeStatus = _enemy.CurrentRange;

        if (rangeStatus == RangeStatus.TooClose || !_enemy.HasLineOfSight)
        {
            _enemy.ChangeState(_enemy.RepositionState);
            return;
        }

        if (rangeStatus == RangeStatus.TooFar)
        {
            _enemy.ChangeState(_enemy.ChaseState);
            return;
        }
        
        // Face the player while attacking.
        Vector3 direction = _enemy.Target.position - _enemy.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            _enemy.transform.rotation = Quaternion.RotateTowards(
                _enemy.transform.rotation,
                Quaternion.LookRotation(direction),
                RotationSpeed * dt
            );
        }

        if (_currentAttack != null && _currentAttack.CanAttack)
            _currentAttack.StartAttack();
    }

    /// <summary>Cancels an unfinished attack and gives rotation back to the NavMeshAgent.</summary>
    public void Exit()
    {
        // Stops an unfinished attack when leaving AttackState
        if (_currentAttack != null)
            _currentAttack.CancelAttack();

        // Restore the NavMeshAgent's original rotation setting
        if  (_agent != null)
            _agent.updateRotation = _previousUpdateRotation;

        _currentAttack = null;
    }
}