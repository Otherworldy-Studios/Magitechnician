using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Moves the enemy with its NavMeshAgent. The enemy states call these methods instead of using the agent directly.
/// Every method does nothing while the agent is off the NavMesh.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent _agent;
    private EnemyBase _enemy; // INTEGRATION: new. Source of the MoveSpeed stat.

    /// <summary>Finds the NavMeshAgent and EnemyBase.</summary>
    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _enemy = GetComponent<EnemyBase>();
    }

    /// <summary>Keeps the agent's speed equal to the MoveSpeed stat.</summary>
    // INTEGRATION: new. EnemyBase.MoveSpeed reads the Stats component, which replaced EnemyStats, so slows and buffs
    // from StatModifiers change the speed while they last.
    private void Update()
    {
        if (_enemy != null)
            _agent.speed = _enemy.MoveSpeed;
    }

    /// <summary>Starts moving toward a point.</summary>
    public void MoveTo(Vector3 destination)
    {
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.SetDestination(destination);
        }
    }

    /// <summary>Stops in place. The destination is kept, so ResumeMoving continues toward it.</summary>
    public void StopMoving()
    {
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = true;
        }
    }

    /// <summary>Continues toward the last destination.</summary>
    public void ResumeMoving()
    {
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = false;
        }
    }

    /// <summary>Moves the enemy to a point at once. Returns false if the agent is off the NavMesh or the warp fails.</summary>
    public bool Warp(Vector3 position)
    {
        return _agent.isOnNavMesh && _agent.Warp(position);
    }

    /// <summary>True once the path is worked out, the enemy is within stopping distance, and it has almost stopped.</summary>
    public bool HasReachedDestination()
    {
        if (!_agent.isOnNavMesh || _agent.pathPending)
            return false;

        return !float.IsInfinity(_agent.remainingDistance)
            && _agent.remainingDistance <= _agent.stoppingDistance
            && (!_agent.hasPath || _agent.velocity.sqrMagnitude < 0.01f);
    }
}
