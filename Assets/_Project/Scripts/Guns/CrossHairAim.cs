using UnityEngine;

//This is the result of aiming one shot at the crosshair.
public readonly struct AimSolution
{
    //Where the shot starts: the muzzle, or teh camera if the muzzle can't reach the aim pooint.
    public readonly Vector3 Origin;

    //Normalized direction from Origin towards AimPoint.
    public readonly Vector3 Direction;

    //The point under the crosshair, or the point at range if crosshair hit nothing.
    public readonly Vector3 AimPoint;

    //True if crosshair ray hit something within range.
    public readonly bool HitSomething;

    //True if the shot leaves from the muzzle; false if it was moved to the camera.
    public readonly bool FromMuzzle;

    public AimSolution(Vector3 origin, Vector3 direction, Vector3 aimPoint, bool hitSomething, bool fromMuzzle)
    {
        Origin = origin;
        Direction = direction;
        AimPoint = aimPoint;
        HitSomething = hitSomething;
        FromMuzzle = fromMuzzle;
    }

}

//Works out where a shot should start and which way it should go so that it lands unedr the crosshair.
public class CrossHairAim
{
    private static readonly Vector3 ViewportCentre = new Vector3(0.5f, 0.5f, 0f);

    //How far the muzzle's first hit may be from where it should touch the aim surface and still count as "the same spot".
    //Only covers floating-point error; anything further away is a different surface in the way.
    //Erring small is safe: it only means the shot moves to the camera.
    private const float AimTolerance = 0.01f;

    //A thick bullet meeting the aim surface at a shallower angle than this (cosine of the angle from
    //the surface normal, about 89 degrees) is fired from the camera instead of predicted.
    private const float MinFacing = 0.02f;

    private static readonly RaycastHit[] s_hits = new RaycastHit[64];

    //Parameter names 
    //"camera" - Camera the crosshair belongs to
    //"muzzle" - World position of the barrel tip
    //"range" - The gun's range measured from the camera
    //"layerMask" - Layers that the bullet cillides with
    //"owner" - Root of whoever fires. Its colliders never block the aim, may be null
    //"bulletRadius" - Radius the bullet sweeos with (0 for a thin ray)
    //"maxMuzzleAngle" - Largest angle, in degrees, between the shot and the crosshair ray before the shot moves to the camera.
    //The default, 90, only does so when the barrel would have to fire sideways or backwards.
    public static AimSolution Solve(Camera camera, Vector3 muzzle, float range, int layerMask, GameObject owner, float bulletRadius = 0f, float maxMuzzleAngle = 90f)
    {
        //A ray from the camera through the screen centre finds the aim point.
        Vector3 throughCentre = camera.ViewportPointToRay(ViewportCentre).direction;
        Ray aimRay = new Ray(camera.transform.position, throughCentre);

        //If that ray hits nothing, the aim point is the point on the ray at the gun's range.
        bool hitSomething = CastAimRay(aimRay, range, layerMask, owner, out Vector3 aimPoint, out Vector3 surfaceNormal);

        //The shot leaves the muzzle and heads straight for the aim point.
        if (MuzzleCanReach(aimRay, muzzle, aimPoint, hitSomething, surfaceNormal, layerMask, owner, bulletRadius, maxMuzzleAngle))
            return new AimSolution(muzzle, (aimPoint - muzzle).normalized, aimPoint, hitSomething, true);

        //If a bullet from the muzzle could not land on the aim point, the shot moves onto the
        //crosshair ray itself (starting at the camera), so it still lands under the crosshair.
        //That only happens when a wall is close:
        //  (a) the aim point is level with or behind the barrel tip (wall closer than the barrel),
        //  (b) the muzzle has poked into or through a wall, or
        //  (c) the muzzle's path hits something the camera can see past (a door frame, a wall edge)
        //      or the muzzle is already touching a wall.
        return new AimSolution(aimRay.origin, aimRay.direction, aimPoint, hitSomething, false);

    }

    //The first thing the crosshair ray hits within range, or the point at range if it hits nothing.
    public static Vector3 FindAimPoint(Ray aimRay, float range, int layerMask, GameObject owner, out bool hitSomething)
    {
        hitSomething = CastAimRay(aimRay, range, layerMask, owner, out Vector3 aimPoint, out _);
        return aimPoint;
    }

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

    //This checks if a bullet leaving the muzzle actually lands on the aim point
    private static bool MuzzleCanReach(Ray aimRay, Vector3 muzzle, Vector3 aimPoint, bool aimHitSurface,
                                      Vector3 surfaceNormal, int layerMask, GameObject owner,
                                      float bulletRadius, float maxMuzzleAngle)
    {
        Vector3 toAim = aimPoint - muzzle;
        float distance = toAim.magnitude;
        if (distance < 0.001f)
            return false;
        Vector3 direction = toAim / distance;

        //(a) Aim point level with or behind the barrel tip: the barrel would fire sideways or backwards.
        if (Vector3.Angle(direction, aimRay.direction) >= maxMuzzleAngle)
            return false;

        //(b) The muzzle is inside or past a wall: something sits between the eye and the muzzle.
        Vector3 eyeToMuzzle = muzzle - aimRay.origin;
        float eyeDistance = eyeToMuzzle.magnitude;
        if (eyeDistance > 0.001f &&
            Sweep(aimRay.origin, eyeToMuzzle / eyeDistance, 0f, eyeDistance, layerMask, owner, out _))
            return false;

        //(c) The bullet's own sweep from the muzzle must first touch the aim surface where expected,
        //  not a door frame, a wall edge, or a wall the muzzle is already touching.
        if (Sweep(muzzle, direction, bulletRadius, distance, layerMask, owner, out RaycastHit hit))
        {
            if (!TryPredictContact(muzzle, direction, distance, aimPoint, aimHitSurface, surfaceNormal,
                                   bulletRadius, out Vector3 expectedContact))
                return false;
            return (hit.point - expectedContact).sqrMagnitude <= AimTolerance * AimTolerance;
        }

        return true;
    }

    //Where a bullet flying from the muzzle toward teh aim point first touches the aim surface, if nothing is in the way.
    //A thin is in the way. A thin bullet touches at the aim point. A thick one touches a slanted wall slightly
    //earlier (radius * tan(angle) before it), while its centre line still runs through the aim point.
    private static bool TryPredictContact(Vector3 muzzle, Vector3 direction, float distance, Vector3 aimPoint, bool aimHitSurface, Vector3 surfaceNormal, float bulletRadius, out Vector3 contact)
    {
        contact = aimPoint;
        if (bulletRadius <= 0f || !aimHitSurface)
            return true;

        float facing = Vector3.Dot(-direction, surfaceNormal);   // cosine of the angle from the surface normal
        if (facing < MinFacing)
            return false;

        Vector3 centreAtContact = muzzle + direction * (distance - bulletRadius / facing);
        contact = centreAtContact - surfaceNormal * bulletRadius;
        return true;
    }

    //Nearest non-trigger hit along a ray (radius 0) or swept sphere, ignoring everything under ignoreRoot.
    //A sphere that starts already touching something reports it at distance 0, at the origin.
    private static bool Sweep(Vector3 origin, Vector3 direction, float radius, float distance, int layerMask, GameObject ignoreRoot, out RaycastHit nearest)
    {
        nearest = default;
        if (distance <= 0f)
            return false;

        int count = radius > 0f
            ? Physics.SphereCastNonAlloc(origin, radius, direction, s_hits, distance, layerMask, QueryTriggerInteraction.Ignore)
            : Physics.RaycastNonAlloc(origin, direction, s_hits, distance, layerMask, QueryTriggerInteraction.Ignore);

        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit candidate = s_hits[i];
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