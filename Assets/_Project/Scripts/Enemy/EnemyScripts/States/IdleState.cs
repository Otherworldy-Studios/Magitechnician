/// <summary>
/// Keeps the enemy stationary until a target is acquired. Transitions to ChaseState when EnemyBase detects the player.
/// </summary>
public class IdleState : IEnemyState
{
    private readonly EnemyBase _enemy;

    /// <summary>Creates the state for one enemy. EnemyBase.Awake calls this.</summary>
    public IdleState(EnemyBase enemy)
    {
        _enemy = enemy;
    }

    /// <summary>Stops enemy movement when entering IdleState.</summary>
    public void Enter()
    {
        _enemy.Movement.StopMoving();
    }

    /// <summary>Waits for a target before transitioning to ChaseState.</summary>
    public void Tick(float dt)
    {
        if (_enemy.HasTarget)
            _enemy.ChangeState(_enemy.ChaseState);
    }

    /// <summary>Does nothing.</summary>
    public void Exit()
    {
    }
}