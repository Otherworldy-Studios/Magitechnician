/// Summary -
/// Keeps the enemy stationary until a target is acquired.
/// Transitions to ChaseState when EnemyBase detects the player.
public class IdleState : IEnemyState
{
    private readonly EnemyBase enemy;

    public IdleState(EnemyBase enemy)
    {
        this.enemy = enemy;
    }
    // Stops enemy movement when entering IdleState.
    public void Enter()
    {
        enemy.Movement.StopMoving();
    }
    // Waits for a target before transitioning to ChaseState.
    public void Update()
    {
        if (enemy.HasTarget)
            enemy.ChangeState(enemy.ChaseState);
    }

    public void Exit()
    {
    }
}