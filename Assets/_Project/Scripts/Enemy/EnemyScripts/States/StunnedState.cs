/// <summary>
/// Represents the state of an enemy when it is stunned.
/// The enemy is unable to move or attack while in this state.
/// </summary>

public class StunnedState : IEnemyState
{
    private readonly EnemyBase enemy;
    private float remainingTime;
    public StunnedState(EnemyBase enemy)
    {
        this.enemy = enemy;
    }

    // The caller supplies the stun duration before entering this state.
    public void SetDuration(float duration)
    {
        remainingTime = UnityEngine.Mathf.Max(0f, duration);
    }

    public void Enter()
    {
        enemy.Movement.StopMoving();
    }

    public void Tick(float dt)
    {
        remainingTime -= dt;
        if(remainingTime > 0f)
            return;
        
        enemy.ChangeState(enemy.HasTarget ? enemy.ChaseState : enemy.IdleState);
    }

    public void Exit(){}
}