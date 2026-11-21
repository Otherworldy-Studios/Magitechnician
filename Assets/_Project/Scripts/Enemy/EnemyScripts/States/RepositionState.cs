using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Moves the enemy away from the player when too close. Uses the Goldilocks range and NavMesh to find a preferred
/// position, then returns to ChaseState. Keeps facing the player while it moves, so a directional blend tree plays
/// walking backward.
/// </summary>
public class RepositionState : IEnemyState
{
    private const float SampleRadius = 2f;      // How far from the wanted spot to look for a point on the NavMesh.
    private const float RepathInterval = 0.25f; // Seconds between new destinations.

    private readonly EnemyBase _enemy;
    private float _nextRepathTime;   // Time.time of the next destination update.
    private bool _previousFacesPath; // EnemyMovement.FacesPath before Enter, put back in Exit.

    public RepositionState(EnemyBase enemy)
    {
        _enemy = enemy;
    }

    /// <summary>Allows immediate position recalculation, stops current movement, plays Reposition, and faces the player.</summary>
    public void Enter()
    {
        _nextRepathTime = 0f;
        _enemy.Movement.StopMoving();
        _enemy.PlayStateAnimation(EnemyAnimation.Reposition);

        _previousFacesPath = _enemy.Movement.FacesPath;
        _enemy.Movement.FacesPath = false;
    }

    /// <summary>
    /// Goes back to Chase when the target is too far, or in range and in sight. Otherwise moves to the point
    /// halfway between MinDistance and MaxDistance from the target, on the side away from it.
    /// </summary>
    public void Tick(float dt)
    {
        if (!_enemy.HasTarget)
        {
            _enemy.ChangeState(_enemy.IdleState);
            return;
        }

        _enemy.FaceTarget(dt);

        RangeStatus status = _enemy.CurrentRange;

        if (status == RangeStatus.TooFar)
        {
            _enemy.ChangeState(_enemy.ChaseState);
            return;
        }

        if (status == RangeStatus.InRange && _enemy.HasLineOfSight)
        {
            _enemy.ChangeState(_enemy.ChaseState);
            return;
        }

        if (_enemy.Range == null)
        {
            _enemy.Movement.StopMoving();
            return;
        }

        if (Time.time < _nextRepathTime)
            return;

        _nextRepathTime = Time.time + RepathInterval;

        Vector3 away = _enemy.transform.position - _enemy.Target.position;
        away.y = 0f;

        if (away.sqrMagnitude < 0.001f)
            away = -_enemy.transform.forward;

        away.Normalize();

        float preferredDistance = (_enemy.Range.MinDistance + _enemy.Range.MaxDistance) * 0.5f;
        Vector3 desiredPosition = _enemy.Target.position + away * preferredDistance;

        if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, SampleRadius, NavMesh.AllAreas))
        {
            _enemy.Movement.MoveTo(hit.position);
        }
        else
        {
            _enemy.Movement.StopMoving();
        }
    }

    /// <summary>Puts EnemyMovement.FacesPath back.</summary>
    public void Exit()
    {
        _enemy.Movement.FacesPath = _previousFacesPath;
    }
}