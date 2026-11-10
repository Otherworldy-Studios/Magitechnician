
/// <summary>
/// Describes the enemy's distance relative to its preferred combat range.
/// </summary>
public enum RangeStatus
{
    TooClose,
    InRange,
    TooFar
}

/// <summary>
/// Contract for the team's Goldilocks range system. Provides the preferred distance boundaries and current range status.
/// </summary>
public interface IEnemyRange
{
    /// <summary>Where the target is compared to the preferred distance.</summary>
    RangeStatus GetRangeStatus();

    /// <summary>Closest preferred distance to the target.</summary>
    float MinDistance { get; }
    /// <summary>Farthest preferred distance to the target.</summary>
    float MaxDistance { get; }
}