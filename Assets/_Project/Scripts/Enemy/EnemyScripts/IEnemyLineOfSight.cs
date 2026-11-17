/// <summary>
/// Contract for the team's line-of-sight detection system. Allows enemy states to check whether the player is visible.
/// </summary>
public interface IEnemyLineOfSight
{
    /// <summary>True if the enemy can see its target.</summary>
    bool HasLineOfSight();
}