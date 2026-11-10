/// <summary>
/// Decides whether the gun fires this frame. Gun.TryFire asks its current fire mode every frame.
/// Because each mode gets both "pressed this frame" and "held", Shoot doesn't need to know which mode is equipped.
/// </summary>
// INTEGRATION: new script. Gun.RefreshFireMode creates the matching class from Stat_FireType or the barrel's FireMode.
public interface IFireMode
{
    /// <summary>Returns true when a shot should fire now.</summary>
    /// <param name="triggerPressed">True only on the frame the fire button went down.</param>
    /// <param name="triggerHeld">True while the fire button is held.</param>
    /// <param name="fireRate">Shots per second, from Stat_FireRate.</param>
    /// <param name="time">The current time, Time.time.</param>
    bool ShouldFire(bool triggerPressed, bool triggerHeld, float fireRate, float time);
}

/// <summary>One shot per press, no faster than the fire rate.</summary>
public class SemiAutoFire : IFireMode
{
    private float _nextShotTime; // The earliest time the next shot can fire.

    /// <inheritdoc />
    public bool ShouldFire(bool triggerPressed, bool triggerHeld, float fireRate, float time)
    {
        if (!triggerPressed || time < _nextShotTime) return false;

        _nextShotTime = time + 1f / fireRate;
        return true;
    }
}

/// <summary>Keeps firing at the fire rate while the button is held.</summary>
public class AutoFire : IFireMode
{
    private float _nextShotTime; // The earliest time the next shot can fire.

    /// <inheritdoc />
    public bool ShouldFire(bool triggerPressed, bool triggerHeld, float fireRate, float time)
    {
        if (!triggerHeld || time < _nextShotTime) return false;

        _nextShotTime = time + 1f / fireRate;
        return true;
    }
}

/// <summary>Each press fires a burst of shots close together, then waits the normal fire rate before the next burst.</summary>
public class BurstFire : IFireMode
{
    private const int ShotsPerBurst = 3;
    private const float BurstSpeedMultiplier = 3f; // Shots inside a burst are 3 times faster than the fire rate.

    private int _shotsLeft;      // Shots left in the current burst. 0 between bursts.
    private float _nextShotTime; // The earliest time the next shot can fire.

    /// <inheritdoc />
    public bool ShouldFire(bool triggerPressed, bool triggerHeld, float fireRate, float time)
    {
        // A press between bursts starts a new burst.
        if (_shotsLeft == 0 && triggerPressed && time >= _nextShotTime)
        {
            _shotsLeft = ShotsPerBurst;
        }

        if (_shotsLeft == 0 || time < _nextShotTime) return false;

        _shotsLeft--;
        // Inside a burst, wait the short interval. After the last shot, wait the normal interval.
        _nextShotTime = _shotsLeft > 0
            ? time + 1f / (fireRate * BurstSpeedMultiplier)
            : time + 1f / fireRate;
        return true;
    }
}
