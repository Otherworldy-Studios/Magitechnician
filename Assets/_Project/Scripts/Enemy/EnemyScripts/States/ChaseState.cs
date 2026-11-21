using UnityEngine;

/// <summary>
/// Moves the enemy toward its target using EnemyMovement. Uses the Goldilocks range and line of sight to decide whether
/// to chase, reposition, or attack. With EnemyBase's Face Target While Chasing on, faces the player instead of its path,
/// so a directional blend tree plays strafing.
/// </summary>
public class ChaseState : IEnemyState
{
    private const float RepathInterval = 0.2f;
    private readonly EnemyBase _enemy;
    private float _nextRepathTime; // Time.time of the next path update. Paths update every RepathInterval seconds.
    private bool _previousFacesPath; // EnemyMovement.FacesPath before Enter, put back in Exit.

    public ChaseState(EnemyBase enemy)
    {
        _enemy = enemy;
    }

    /// <summary>Allows immediate path recalculation, resumes movement, and plays Chase.</summary>
    public void Enter()
    {
        _nextRepathTime = 0f;
        _enemy.Movement.ResumeMoving();
        _enemy.PlayStateAnimation(EnemyAnimation.Chase);

        _previousFacesPath = _enemy.Movement.FacesPath;
        if (_enemy.FaceTargetWhileChasing)
            _enemy.Movement.FacesPath = false;
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

        if (_enemy.FaceTargetWhileChasing)
            _enemy.FaceTarget(dt);

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

    /// <summary>Puts EnemyMovement.FacesPath back.</summary>
    public void Exit()
    {
        _enemy.Movement.FacesPath = _previousFacesPath;
    }
}