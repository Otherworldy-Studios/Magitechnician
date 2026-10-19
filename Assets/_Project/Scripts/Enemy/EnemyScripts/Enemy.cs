
using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]

// TEMPORARY: Used to test NavMeshAgent movement in the sandbox scene.
// Currently moves between waypoints to verify navigation and obstacle avoidance.
// This behavior will be replaced or integrated with EnemyBase once
// the enemy state machine is implemented.

public class Enemy : MonoBehaviour
{
    [SerializeField] private Transform[] waypoints;

    private EnemyMovement movement;
    private int currentWaypoint = 0;

    void Start()
    {
        movement = GetComponent<EnemyMovement>();

        if (waypoints.Length > 0)
        {
            movement.MoveTo(waypoints[currentWaypoint].position);
        }
    }

    void Update()
    {
        if (waypoints.Length == 0)
            return;

        if (movement.HasReachedDestination())
        {
            currentWaypoint = (currentWaypoint + 1) % waypoints.Length;

            movement.MoveTo(waypoints[currentWaypoint].position);
        }
    }
}
