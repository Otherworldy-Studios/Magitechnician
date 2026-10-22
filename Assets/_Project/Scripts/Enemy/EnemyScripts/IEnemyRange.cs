
/// Summary -
/// Describes the enemy's distance relative to its preferred combat range.
public enum RangeStatus
{
    TooClose,
    InRange,
    TooFar
}

/// Summary -
/// Contract for the team's Goldilocks range system.
/// Provides the preferred distance boundaries and current range status.
public interface IEnemyRange
{
    RangeStatus GetRangeStatus();

    float MinDistance { get; }
    float MaxDistance { get; }
}