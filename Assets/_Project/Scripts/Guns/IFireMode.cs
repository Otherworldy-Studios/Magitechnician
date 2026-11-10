
public interface IFireMode
{
    // triggerPressed is true only on the frame the fire button went down. triggerHeld stays true while it's held.
    // fireRate is shots per second, from Stat_FireRate.
    bool ShouldFire(bool triggerPressed, bool triggerHeld, float fireRate, float time);
}

// One shot per press, no faster than the fire rate.
public class SemiAutoFire : IFireMode
{
    private float _nextShotTime;

    public bool ShouldFire(bool triggerPressed, bool triggerHeld, float fireRate, float time)
    {
        if (!triggerPressed || time < _nextShotTime) return false;

        _nextShotTime = time + 1f / fireRate;
        return true;
    }
}

// Keeps firing at the fire rate while the button is held.
public class AutoFire : IFireMode
{
    private float _nextShotTime;

    public bool ShouldFire(bool triggerPressed, bool triggerHeld, float fireRate, float time)
    {
        if (!triggerHeld || time < _nextShotTime) return false;

        _nextShotTime = time + 1f / fireRate;
        return true;
    }
}

// Each press fires a burst of shots close together, then waits the normal fire rate before the next burst.
public class BurstFire : IFireMode
{
    private const int ShotsPerBurst = 3;
    private const float BurstSpeedMultiplier = 3f; // shots inside a burst are 3 times faster than the fire rate

    private int _shotsLeft;
    private float _nextShotTime;

    public bool ShouldFire(bool triggerPressed, bool triggerHeld, float fireRate, float time)
    {
        if (_shotsLeft == 0 && triggerPressed && time >= _nextShotTime)
        {
            _shotsLeft = ShotsPerBurst;
        }

        if (_shotsLeft == 0 || time < _nextShotTime) return false;

        _shotsLeft--;
        _nextShotTime = _shotsLeft > 0
            ? time + 1f / (fireRate * BurstSpeedMultiplier)
            : time + 1f / fireRate;
        return true;
    }
}