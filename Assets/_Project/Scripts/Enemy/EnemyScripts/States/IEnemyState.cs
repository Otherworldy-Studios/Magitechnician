/// <summary>
/// Defines the common lifecycle methods for all enemy states. Allows EnemyBase to switch states without depending on their specific implementations.
/// </summary>
public interface IEnemyState
{
    /// <summary>Runs once when EnemyBase.ChangeState switches to this state.</summary>
    void Enter();
    /// <summary>Runs every frame from EnemyBase.Update while this is the current state.</summary>
    void Tick(float dt);
    /// <summary>Runs once when EnemyBase.ChangeState switches away from this state.</summary>
    void Exit();
}