
using UnityEngine;

/// Summary - 
/// Represents the enemy's terminal state after death.
/// Stops movement and prevents further enemy behavior.
/// Entered when EnemyBase receives Healthbar's death event.
public class DeadState : IEnemyState
{
    private readonly EnemyBase enemy;

    public DeadState(EnemyBase enemy)
    {
        this.enemy = enemy;
    }
    // Stops movement and logs the enemy's death.
    public void Enter()
    {
        enemy.Movement.StopMoving();

        Debug.Log($"{enemy.name} died");

        // TODO: Trigger death animation and cleanup.
    }
    // Dead enemies perform no further behavior.
    public void Update()
    {
    }

    public void Exit()
    {
    }
}