using UnityEngine;

/// <summary>The result of aiming one shot at the crosshair: where the shot starts and which way it goes.</summary>
public readonly struct AimSolution
{
    /// <summary>Where the shot starts: the muzzle, or the camera if the muzzle can't reach the aim point.</summary>
    public Vector3 Origin { get; }

    /// <summary>Normalized direction from Origin toward AimPoint.</summary>
    public Vector3 Direction { get; }

    /// <summary>The point under the crosshair, or the point at range if the crosshair hit nothing. CrosshairDot draws the dot here.</summary>
    public Vector3 AimPoint { get; }

    /// <summary>True if the crosshair ray hit something within range.</summary>
    public bool HitSomething { get; }

    /// <summary>True if the shot leaves from the muzzle; false if it was moved to the camera.</summary>
    public bool FromMuzzle { get; }

    /// <summary>Creates a solution. Only CrossHairAim.Solve needs to call this.</summary>
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
/// Works out where a shot should start and which way it should go so that it lands under the crosshair.
/// The aim point always comes from the camera's ray through the screen center. The shot starts at the muzzle
/// when a bullet from there would land on that point, and from the camera when a wall is in the way, so shots
/// can't pass through a wall in front of the gun.
/// </summary>
// INTEGRATION: unchanged in logic, but now used: Shoot calls Solve every frame and passes the result to Gun.TryFire,
// and CrosshairDot draws the dot at the result's AimPoint. It replaced PlayerAimController.
public class CrossHairAim
{
    private static readonly Vector3 ViewportCentre = new Vector3(0.5f, 0.5f, 0f);

    // How far the muzzle's first hit may be from where it should touch the aim surface and still count as the same spot.
    // Only covers floating-point error; anything further away is a different surface in the way.
    // Erring small is safe: it only means the shot moves to the camera.
    private const float AimTolerance = 0.01f;

    // A thick bullet meeting the aim surface at a shallower angle than this (cosine of the angle from
    // the surface normal, about 89 degrees) is fired from the camera instead of predicted.
    private const float MinFacing = 0.02f;

    // Shared buffer for the casts, so they don't allocate.
    private static readonly RaycastHit[] _hits = new RaycastHit[64];

    /// <summary>Works out the shot for this frame.</summary>
    /// <param name="camera">The camera the crosshair belongs to.</param>
    /// <param name="muzzle">World position of the barrel tip.</param>
    /// <param name="range">The gun's range, measured from the camera.</param>
    /// <param name="layerMask">Layers the bullet collides with.</param>
    /// <param name="owner">Root of whoever fires. Its colliders never block the aim. May be null.</param>
    /// <param name="bulletRadius">Radius the bullet sweeps with. 0 for a thin ray.</param>
    /// <param name="maxMuzzleAngle">
    /// Largest angle, in degrees, between the shot and the crosshair ray before the shot moves to the camera.
    /// The default, 90, only does so when the barrel would have to fire sideways or backwards.
    /// </param>
    public static AimSolution Solve(Camera camera, Vector3 muzzle, float range, int layerMask, GameObject owner, float bulletRadius = 0f, float maxMuzzleAngle = 90f)
    {
        // A ray from the camera through the screen center finds the aim point.
        Vector3 throughCentre = camera.ViewportPointToRay(ViewportCentre).direction;
        Ray aimRay = new Ray(camera.transform.position, throughCentre);

        // If that ray hits nothing, the aim point is the point on the ray at the gun's range.
        bool hitSomething = CastAimRay(aimRay, range, layerMask, owner, out Vector3 aimPoint, out Vector3 surfaceNormal);

        // The shot leaves the muzzle and heads straight for the aim point.
        if (MuzzleCanReach(aimRay, muzzle, aimPoint, hitSomething, surfaceNormal, layerMask, owner, bulletRadius, maxMuzzleAngle))
            return new AimSolution(muzzle, (aimPoint - muzzle).normalized, aimPoint, hitSomething, true);

        // If a bullet from the muzzle could not land on the aim point, the shot moves onto the
        // crosshair ray itself (starting at the camera), so it still lands under the crosshair.
        // That only happens when a wall is close:
        //  (a) the aim point is level with or behind the barrel tip (wall closer than the barrel),
        //  (b) the muzzle has poked into or through a wall, or
        //  (c) the muzzle's path hits something the camera can see past (a door frame, a wall edge)
        //      or the muzzle is already touching a wall.
        return new AimSolution(aimRay.origin, aimRay.direction, aimPoint, hitSomething, false);
    }

    /// <summary>Returns the first thing the crosshair ray hits within range, or the point at range if it hits nothing.</summary>
    public static Vector3 FindAimPoint(Ray aimRay, float range, int layerMask, GameObject owner, out bool hitSomething)
    {
        hitSomething = CastAimRay(aimRay, range, layerMask, owner, out Vector3 aimPoint, out _);
        return aimPoint;
    }

    /// <summary>Casts the crosshair ray. Returns true and the hit point and normal if it hits something within range.</summary>
    private static bool CastAimRay(Ray aimRay, float range, int layerMask, GameObject owner,
                                   out Vector3 aimPoint, out Vector3 surfaceNormal)
    {
        if (Sweep(aimRay.origin, aimRay.direction, 0f, range, layerMask, owner, out RaycastHit hit))
        {
            aimPoint = hit.point;
            surfaceNormal = hit.normal;
            return true;
        }

        aimPoint = aimRay.GetPoint(range);
        surfaceNormal = Vector3.zero;
        return false;
    }

    /// <summary>Checks whether a bullet leaving the muzzle actually lands on the aim point. See the three cases in Solve.</summary>
    private static bool MuzzleCanReach(Ray aimRay, Vector3 muzzle, Vector3 aimPoint, bool aimHitSurface,
                                      Vector3 surfaceNormal, int layerMask, GameObject owner,
                                      float bulletRadius, float maxMuzzleAngle)
    {
        Vector3 toAim = aimPoint - muzzle;
        float distance = toAim.magnitude;
        if (distance < 0.001f)
            return false;
        Vector3 direction = toAim / distance;

        // (a) Aim point level with or behind the barrel tip: the barrel would fire sideways or backwards.
        if (Vector3.Angle(direction, aimRay.direction) >= maxMuzzleAngle)
            return false;

        // (b) The muzzle is inside or past a wall: something sits between the eye and the muzzle.
        Vector3 eyeToMuzzle = muzzle - aimRay.origin;
        float eyeDistance = eyeToMuzzle.magnitude;
        if (eyeDistance > 0.001f &&
            Sweep(aimRay.origin, eyeToMuzzle / eyeDistance, 0f, eyeDistance, layerMask, owner, out _))
            return false;

        // (c) The bullet's own sweep from the muzzle must first touch the aim surface where expected,
        //     not a door frame, a wall edge, or a wall the muzzle is already touching.
        if (Sweep(muzzle, direction, bulletRadius, distance, layerMask, owner, out RaycastHit hit))
        {
            if (!TryPredictContact(muzzle, direction, distance, aimPoint, aimHitSurface, surfaceNormal,
                                   bulletRadius, out Vector3 expectedContact))
                return false;
            return (hit.point - expectedContact).sqrMagnitude <= AimTolerance * AimTolerance;
        }

        return true;
    }

    /// <summary>
    /// Predicts where a bullet flying from the muzzle toward the aim point first touches the aim surface, if nothing
    /// is in the way. A thin bullet touches at the aim point. A thick one touches a slanted wall slightly earlier
    /// (radius × tan(angle) before it), while its center line still runs through the aim point.
    /// Returns false when the angle is too shallow to predict.
    /// </summary>
    private static bool TryPredictContact(Vector3 muzzle, Vector3 direction, float distance, Vector3 aimPoint, bool aimHitSurface, Vector3 surfaceNormal, float bulletRadius, out Vector3 contact)
    {
        contact = aimPoint;
        if (bulletRadius <= 0f || !aimHitSurface)
            return true;

        float facing = Vector3.Dot(-direction, surfaceNormal); // Cosine of the angle from the surface normal.
        if (facing < MinFacing)
            return false;

        Vector3 centreAtContact = muzzle + direction * (distance - bulletRadius / facing);
        contact = centreAtContact - surfaceNormal * bulletRadius;
        return true;
    }

    /// <summary>
    /// Finds the nearest non-trigger hit along a ray (radius 0) or a swept sphere, ignoring everything under ignoreRoot.
    /// A sphere that starts already touching something reports it at distance 0, at the origin.
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

        // A sphere that begins overlapping a collider is reported at distance 0 with point (0,0,0).
        if (found && nearest.distance <= 0f)
            nearest.point = origin;

        return found;
    }
}
