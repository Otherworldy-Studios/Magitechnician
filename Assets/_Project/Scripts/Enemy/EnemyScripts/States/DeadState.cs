
using UnityEngine;

/// <summary>
/// Represents the enemy's terminal state after death. Stops movement and prevents further enemy behavior. Entered through EnemyBase.Die, when Health fires OnDied.
/// </summary>
public class DeadState : IEnemyState
{
    private readonly EnemyBase _enemy;

    /// <summary>Creates the state for one enemy. EnemyBase.Awake calls this.</summary>
    public DeadState(EnemyBase enemy)
    {
        _enemy = enemy;
    }

    /// <summary>Disables enemy behavior and collisions, then removes the enemy</summary>
    public void Enter()
    {
        _enemy.Movement.StopMoving();
        _enemy.Movement.enabled = false;

        // Disable enemy collisions
        foreach (Collider collider in _enemy.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        // Disable AI
        _enemy.enabled = false;

        // Remove the enemy
        Object.Destroy(_enemy.gameObject);
    }

    /// <summary>Dead enemies perform no further behavior.</summary>
    public void Tick(float dt)
    {
    }

    /// <summary>Does nothing. EnemyBase.ChangeState never leaves DeadState.</summary>
    public void Exit()
    {
    }
}