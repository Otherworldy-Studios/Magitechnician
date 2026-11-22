using UnityEngine;

/// <summary>The result of aiming one shot at the crosshair: where the shot starts and which way it goes.</summary>
public readonly struct AimSolution
{
    /// <summary>Where the shot starts: the muzzle, or a point on the crosshair ray beside the gun when the muzzle is blocked.</summary>
    public Vector3 Origin { get; }

    /// <summary>Normalized direction from Origin toward AimPoint.</summary>
    public Vector3 Direction { get; }

    /// <summary>The point under the crosshair, or the point at range if the crosshair hit nothing.</summary>
    public Vector3 AimPoint { get; }

    /// <summary>True if the crosshair ray hit something within range.</summary>
    public bool HitSomething { get; }

    /// <summary>True if the shot leaves from the muzzle. False when it starts on the crosshair ray instead.</summary>
    public bool FromMuzzle { get; }

    public AimSolution(Vector3 origin, Vector3 direction, Vector3 aimPoint, bool hitSomething, bool fromMuzzle)
    {
        Origin = origin;
        Direction = direction;
        AimPoint = aimPoint;
        HitSomething = hitSomething;
        FromMuzzle = fromMuzzle;
    }
}

/// <summary>
/// Aims third-person shots so they land under the crosshair at the screen center. The aim point comes from the camera's
/// ray through the screen center, started level with the gun, so nothing between the camera and the player can catch
/// it. The shot flies from the muzzle to that point whenever the path is clear. When something blocks the muzzle's path
/// before the aim point, such as a corner next to the player's shoulder, the shot starts on the crosshair ray beside the
/// gun instead, so the player never loses a shot to a wall they can't see past.
/// </summary>
public static class CrossHairAim
{
    private static readonly Vector3 ViewportCentre = new Vector3(0.5f, 0.5f, 0f);

    // A hit on the muzzle's path within this many meters of the aim point counts as reaching it. Covers the bullet's
    // radius touching a curved target, such as an enemy's capsule, slightly before its center line would.
    private const float ReachTolerance = 0.3f;

    // Aim points closer to the muzzle than this, such as a wall the gun is pressed against, use the crosshair ray.
    private const float MinMuzzleDistance = 0.1f;

    private static readonly RaycastHit[] _hits = new RaycastHit[64]; // Shared buffer, so the casts don't allocate.

    /// <summary>Works out this frame's shot. Shoot calls it every frame and passes the result to Gun.TryFire.</summary>
    /// <param name="camera">The camera the crosshair belongs to.</param>
    /// <param name="muzzle">World position of the barrel tip.</param>
    /// <param name="range">The gun's range, measured from where the aim ray starts, level with the gun.</param>
    /// <param name="layerMask">Layers the bullet collides with.</param>
    /// <param name="owner">Root of whoever fires. Its colliders never block the aim. Can be null.</param>
    /// <param name="bulletRadius">Radius the bullet sweeps with. 0 for a thin ray.</param>
    /// <param name="maxMuzzleAngle">
    /// Largest angle, in degrees, between the muzzle's path and the crosshair ray before the shot starts on the
    /// crosshair ray instead. 90 only does so when the gun would have to fire sideways or backward.
    /// </param>
    public static AimSolution Solve(Camera camera, Vector3 muzzle, float range, int layerMask, GameObject owner, float bulletRadius = 0f, float maxMuzzleAngle = 90f)
    {
        Ray cameraRay = camera.ViewportPointToRay(ViewportCentre);

        // Starting level with the muzzle skips anything between the camera and the gun, such as the player's back
        // or a wall behind the shoulder.
        float depthOfMuzzle = Mathf.Max(0f, Vector3.Dot(muzzle - cameraRay.origin, cameraRay.direction));
        Vector3 aimStart = cameraRay.GetPoint(depthOfMuzzle);

        bool hitSomething = Sweep(aimStart, cameraRay.direction, 0f, range, layerMask, owner, out RaycastHit aimHit);
        Vector3 aimPoint = hitSomething ? aimHit.point : aimStart + cameraRay.direction * range;

        if (MuzzleCanReach(muzzle, aimStart, aimPoint, cameraRay.direction, layerMask, owner, bulletRadius, maxMuzzleAngle))
            return new AimSolution(muzzle, (aimPoint - muzzle).normalized, aimPoint, hitSomething, true);

        return new AimSolution(aimStart, cameraRay.direction, aimPoint, hitSomething, false);
    }

    /// <summary>
    /// True when a bullet from the muzzle gets to the aim point: the aim point is in front of the gun and not right
    /// against it, the muzzle hasn't poked through a wall, and nothing on the bullet's path is hit well before the
    /// aim point.
    /// </summary>
    private static bool MuzzleCanReach(Vector3 muzzle, Vector3 aimStart, Vector3 aimPoint, Vector3 aimDirection,
                                       int layerMask, GameObject owner, float bulletRadius, float maxMuzzleAngle)
    {
        Vector3 toAim = aimPoint - muzzle;
        float distance = toAim.magnitude;

        if (distance < MinMuzzleDistance)
            return false;

        Vector3 direction = toAim / distance;

        if (Vector3.Angle(direction, aimDirection) >= maxMuzzleAngle)
            return false;

        // The muzzle is through a wall when a wall sits between it and the aim ray's start beside it.
        Vector3 startToMuzzle = muzzle - aimStart;
        float startToMuzzleDistance = startToMuzzle.magnitude;
        if (startToMuzzleDistance > 0.001f &&
            Sweep(aimStart, startToMuzzle / startToMuzzleDistance, 0f, startToMuzzleDistance, layerMask, owner, out _))
            return false;

        if (!Sweep(muzzle, direction, bulletRadius, distance, layerMask, owner, out RaycastHit hit))
            return true;

        return hit.distance >= distance - bulletRadius - ReachTolerance;
    }

    /// <summary>
    /// Finds the nearest non-trigger hit along a ray (radius 0) or a swept sphere, ignoring everything under ignoreRoot.
    /// A sphere that starts already touching something reports it at distance 0.
    /// </summary>
    private static bool Sweep(Vector3 origin, Vector3 direction, float radius, float distance, int layerMask, GameObject ignoreRoot, out RaycastHit nearest)
    {
        nearest = default;
        if (distance <= 0f)
            return false;

        int count = radius > 0f
            ? Physics.SphereCastNonAlloc(origin, radius, direction, _hits, distance, layerMask, QueryTriggerInteraction.Ignore)
            : Physics.RaycastNonAlloc(origin, direction, _hits, distance, layerMask, QueryTriggerInteraction.Ignore);

        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit candidate = _hits[i];

            if (ignoreRoot != null && candidate.collider.transform.IsChildOf(ignoreRoot.transform))
                continue;

            if (!found || candidate.distance < nearest.distance)
            {
                nearest = candidate;
                found = true;
            }
        }

        // A sphere that begins overlapping a collider is reported at distance 0 with point (0, 0, 0).
        if (found && nearest.distance <= 0f)
            nearest.point = origin;

        return found;
    }
}