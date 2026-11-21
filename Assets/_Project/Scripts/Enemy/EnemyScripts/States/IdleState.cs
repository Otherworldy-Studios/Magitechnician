/// <summary>
/// Keeps the enemy stationary until a target is acquired. Transitions to ChaseState when EnemyBase detects the player.
/// </summary>
public class IdleState : IEnemyState
{
    private readonly EnemyBase _enemy;

    public IdleState(EnemyBase enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        _enemy.Movement.StopMoving();
        _enemy.PlayStateAnimation(EnemyAnimation.Idle);
    }

    /// <summary>Waits for a target before transitioning to ChaseState.</summary>
    public void Tick(float dt)
    {
        if (_enemy.HasTarget)
            _enemy.ChangeState(_enemy.ChaseState);
    }

    public void Exit()
    {
    }
}