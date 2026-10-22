/// Summary -
/// Contract for the team's line-of-sight detection system.
/// Allows enemy states to check whether the player is visible.
public interface IEnemyLineOfSight
{
    bool HasLineOfSight();
}