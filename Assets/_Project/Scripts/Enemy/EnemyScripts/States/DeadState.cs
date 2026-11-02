
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
    // Disables enemy behavior and collisions, then removes the enemy
    public void Enter()
    {
        enemy.Movement.StopMoving();
        enemy.Movement.enabled = false;

        // Disable enemy collisions
        foreach (Collider collider in enemy.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        // Disable AI
        enemy.enabled = false;

        // Remove the enemy
        Object.Destroy(enemy.gameObject);
    }
    // Dead enemies perform no further behavior.
    public void Update()
    {
    }

    public void Exit()
    {
    }
}