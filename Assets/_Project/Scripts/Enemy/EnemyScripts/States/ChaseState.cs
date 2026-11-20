using UnityEngine;
/// <summary>
/// Moves the enemy toward its target using EnemyMovement.cs. Uses Goldilocks range and Line of Sight (LOS) to determine whether to chase, reposition, or attack.
/// </summary>
public class ChaseState : IEnemyState
{
    private readonly EnemyBase _enemy;
    private const float RepathInterval = 0.2f;
    private float _nextRepathTime; // Time.time of the next path update. Paths update every RepathInterval seconds.

    /// <summary>Creates the state for one enemy. EnemyBase.Awake calls this.</summary>
    public ChaseState(EnemyBase enemy)
    {
        _enemy = enemy;
    }

    /// <summary>Allows immediate path recalculation and resumes movement.</summary>
    public void Enter()
    {
        _nextRepathTime = 0f;
        _enemy.Movement.ResumeMoving();
    }

    /// <summary>
    /// Moves toward the target while it's too far. Switches to Reposition when too close or out of sight,
    /// to Attack when in range and in sight, and to Idle when the target is lost.
    /// </summary>
    public void Tick(float dt)
    {
        if (!_enemy.HasTarget)
        {
            _enemy.ChangeState(_enemy.IdleState);
            return;
        }

        switch (_enemy.CurrentRange)
        {
            case RangeStatus.TooClose:
                _enemy.ChangeState(_enemy.RepositionState);
                return;

            case RangeStatus.InRange:
                if (!_enemy.HasLineOfSight)
                {
                    _enemy.ChangeState(_enemy.RepositionState);
                    return;
                }

                _enemy.Movement.StopMoving();
                _enemy.ChangeState(_enemy.AttackState);

                return;

            case RangeStatus.TooFar:
                if (Time.time >= _nextRepathTime)
                {
                    _nextRepathTime = Time.time + RepathInterval;
                    _enemy.Movement.MoveTo(_enemy.Target.position);
                }
                return;
        }
    }

    /// <summary>Does nothing.</summary>
    public void Exit()
    {
    }
}