/// <summary>
/// Represents the state of an enemy when it is stunned.
/// The enemy is unable to move or attack while in this state.
/// </summary>
public class StunnedState : IEnemyState
{
    private readonly EnemyBase _enemy;
    private float _remainingTime; // Seconds of stun left. Set by SetDuration.
    /// <summary>Creates the state for one enemy. EnemyBase.Awake calls this.</summary>
    public StunnedState(EnemyBase enemy)
    {
        _enemy = enemy;
    }

    /// <summary>Sets how long the stun lasts. EnemyBase.Stun calls this before entering the state.</summary>
    public void SetDuration(float duration)
    {
        _remainingTime = UnityEngine.Mathf.Max(0f, duration);
    }

    /// <summary>Stops moving.</summary>
    public void Enter()
    {
        _enemy.Movement.StopMoving();
    }

    /// <summary>Counts down the stun, then goes to Chase if there's a target, or Idle if not.</summary>
    public void Tick(float dt)
    {
        _remainingTime -= dt;
        if(_remainingTime > 0f)
            return;
        
        _enemy.ChangeState(_enemy.HasTarget ? _enemy.ChaseState : _enemy.IdleState);
    }

    /// <summary>Does nothing.</summary>
    public void Exit() { }
}