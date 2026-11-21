using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Represents the enemy's terminal state after death. Stops movement and prevents further enemy behavior. Entered
/// through EnemyBase.Die, when Health fires OnDied.
/// </summary>
public class DeadState : IEnemyState
{
    private readonly EnemyBase _enemy;

    /// <summary>Creates the state for one enemy. EnemyBase.Awake calls this.</summary>
    public DeadState(EnemyBase enemy)
    {
        _enemy = enemy;
    }

    /// <summary>Plays Dead, disables enemy behavior and collisions, then removes the enemy after its Death Delay.</summary>
    public void Enter()
    {
        _enemy.PlayStateAnimation(EnemyAnimation.Dead);

        _enemy.Movement.StopMoving();
        _enemy.Movement.enabled = false;

        // Turn the NavMeshAgent off right away, so the enemy doesn't try to move while waiting for the death delay.
        NavMeshAgent agent = _enemy.GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.enabled = false;

        foreach (Collider collider in _enemy.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        _enemy.enabled = false;

        Object.Destroy(_enemy.gameObject, _enemy.DeathDelay);
    }

    public void Tick(float dt)
    {
    }

    public void Exit()
    {
    }
}