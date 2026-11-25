using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Moves the enemy toward its target using EnemyMovement. Uses the Goldilocks range and line of sight to decide whether
/// to chase, reposition, or attack. With EnemyBase's Face Target While Chasing on, faces the player instead of its path,
/// so a directional blend tree plays strafing.
/// </summary>
public class ChaseState : IEnemyState
{
    /// <summary>
    /// How far inside MaxRange the enemy walks before it attacks. AttackState uses the same value past MaxRange
    /// before it goes back to Chase.
    /// </summary>
    public const float AttackMargin = 0.75f;

    private const float RepathInterval = 0.2f;
    private const float SampleRadius = 2f;

    private readonly EnemyBase _enemy;
    private float _nextRepathTime; // Time.time of the next path update. Paths update every RepathInterval seconds.
    private bool _previousFacesPath; // EnemyMovement.FacesPath before Enter, put back in Exit.
    private float _desiredAngle; // Angle in radians on the circle around the player that this enemy walks to.
    private bool _moveStraightAtTarget;

    public ChaseState(EnemyBase enemy)
    {
        _enemy = enemy;
    }

    /// <summary>Delays the first path update by a random amount so enemies don't all repath on the same frame, resumes movement, and plays Chase.</summary>
    public void Enter()
    {
        _nextRepathTime = Time.time + Random.Range(0f, RepathInterval);
        _moveStraightAtTarget = false;

        _enemy.Movement.ResumeMoving();
        _enemy.PlayStateAnimation(EnemyAnimation.Chase);
        _desiredAngle = CalculateDesiredAngle();
        _previousFacesPath = _enemy.Movement.FacesPath;
        if (_enemy.FaceTargetWhileChasing)
            _enemy.Movement.FacesPath = false;
    }

    /// <summary>Returns the angle from the player to the enemy, shifted by up to 90 degrees either way so enemies fan out.</summary>
    private float CalculateDesiredAngle()
    {
        float randomOffset = Random.Range(-90f, 90f) * Mathf.Deg2Rad;

        Vector2 fromPlayer = new Vector2(
            _enemy.transform.position.x - _enemy.Target.position.x,
            _enemy.transform.position.z - _enemy.Target.position.z);
        float angle = Mathf.Atan2(fromPlayer.y, fromPlayer.x);
        return angle + randomOffset;
    }

    /// <summary>
    /// Moves toward the target while it's too far, or in range but not yet AttackMargin inside it. Switches to
    /// Reposition when too close or out of sight, to Attack once in sight and past the margin inside its range,
    /// and to Idle when the target is lost.
    /// </summary>
    public void Tick(float dt)
    {
        if (!_enemy.HasTarget)
        {
            _enemy.ChangeState(_enemy.IdleState);
            return;
        }

        float distance = Vector3.Distance(_enemy.Target.position, _enemy.transform.position);

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

                if (distance <= GetAttackDistance())
                {
                    _enemy.Movement.StopMoving();
                    _enemy.ChangeState(_enemy.AttackState);
                    return;
                }

                RepathIfDue();
                return;

            case RangeStatus.TooFar:
                RepathIfDue();
                return;
        }
    }

    private float GetAttackDistance()
    {
        float middleOfRange = (_enemy.MinRange + _enemy.MaxRange) / 2f;
        return Mathf.Max(_enemy.MaxRange - AttackMargin, middleOfRange);
    }

    private void RepathIfDue()
    {
        if (Time.time < _nextRepathTime)
            return;

        _nextRepathTime = Time.time + RepathInterval;

        if (!_moveStraightAtTarget && _enemy.Movement.HasReachedDestination())
            _moveStraightAtTarget = true;

        _enemy.Movement.MoveTo(_moveStraightAtTarget ? _enemy.Target.position : GetPointAroundPlayer());
    }

    private Vector3 GetPointAroundPlayer()
    {
        Vector3 desiredPoint = Vector3.zero;
        float radius = (_enemy.MinRange + _enemy.MaxRange) / 2f;

        desiredPoint.x = _enemy.Target.position.x + radius * Mathf.Cos(_desiredAngle);
        desiredPoint.y = _enemy.Target.position.y;
        desiredPoint.z = _enemy.Target.position.z + radius * Mathf.Sin(_desiredAngle);

        if (NavMesh.SamplePosition(desiredPoint, out NavMeshHit hit, SampleRadius, NavMesh.AllAreas))
            return hit.position;

        return _enemy.Target.position;
    }

    public void Exit()
    {
        _enemy.Movement.FacesPath = _previousFacesPath;
    }
}