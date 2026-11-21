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
    private EnemyBase _enemy; // Source of the MoveSpeed stat.

    /// <summary>
    /// Multiplies the MoveSpeed stat, such as 0.3 to slow down or 0 to stop. BlockReaction lowers it while the Dark
    /// Knight blocks. 1 by default.
    /// </summary>
    public float SpeedMultiplier { get; set; } = 1f;

    /// <summary>
    /// True when the NavMeshAgent turns the enemy toward its path. States that face the player instead, such as
    /// AttackState and RepositionState, turn it off in Enter and put it back in Exit.
    /// </summary>
    public bool FacesPath
    {
        get => _agent.updateRotation;
        set => _agent.updateRotation = value;
    }

    
    public Vector3 Velocity => _agent.velocity;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _enemy = GetComponent<EnemyBase>();
    }

    private void Update()
    {
        // Read every frame, so slows and buffs from StatModifiers change the speed while they last.
        if (_enemy != null)
            _agent.speed = _enemy.MoveSpeed * SpeedMultiplier;
    }

    public void MoveTo(Vector3 destination)
    {
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.SetDestination(destination);
        }
    }

    public void StopMoving()
    {
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = true;
        }
    }

    public void ResumeMoving()
    {
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = false;
        }
    }

    public bool Warp(Vector3 position)
    {
        return _agent.isOnNavMesh && _agent.Warp(position);
    }

    public bool HasReachedDestination()
    {
        if (!_agent.isOnNavMesh || _agent.pathPending)
            return false;

        return !float.IsInfinity(_agent.remainingDistance)
            && _agent.remainingDistance <= _agent.stoppingDistance
            && (!_agent.hasPath || _agent.velocity.sqrMagnitude < 0.01f);
    }
}
