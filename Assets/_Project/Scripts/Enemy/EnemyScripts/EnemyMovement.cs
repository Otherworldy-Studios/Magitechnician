
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent agent;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public void MoveTo(Vector3 destination)
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
        }
    }

    public void StopMoving()
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    }

    public void ResumeMoving()
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    public bool HasReachedDestination()
    {
        if (!agent.isOnNavMesh || agent.pathPending)
            return false;

        return !float.IsInfinity(agent.remainingDistance)
            && agent.remainingDistance <= agent.stoppingDistance
            && (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f);
    }
}
