/// Summary -
/// Defines the common lifecycle methods for all enemy states.
/// Allows EnemyBase to switch states without depending
/// on their specific implementations.
public interface IEnemyState
{
    void Enter();
    void Update();
    void Exit();
}