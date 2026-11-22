using UnityEngine;

/// <summary>
/// Launches a cannonball in an arc toward the player's feet, so it lands where the player was standing. The Skeletal
/// Orc uses this. Shows an optional marker where the cannonball will land, and spawns an optional HealingPool where it
/// lands.
/// </summary>
public class ArcProjectileAttack : EnemyAttack
{
    [SerializeField] private Projectile _cannonballPrefab;
    [SerializeField] private Transform _firePoint;

    [Tooltip("The preferred launch angle. Raised when the target is above the fire point.")]
    [SerializeField, Range(10f, 85f)] private float _launchAngle = 55f;
    [Tooltip("Meters the cannonball travels before it's destroyed.")]
    [SerializeField] private float _projectileRange = 100f;

    [Header("Landing")]
    [Tooltip("Shown on the ground at the landing point while the cannonball is in the air. Optional.")]
    [SerializeField] private GameObject _landingMarkerPrefab;
    [Tooltip("Spawned on the ground where the cannonball lands. Optional.")]
    [SerializeField] private HealingPool _healingPoolPrefab;
    [Tooltip("What counts as ground for the marker and the pool.")]
    [SerializeField] private LayerMask _groundLayers = ~0;
    [Tooltip("How far below the landing point to look for ground.")]
    [SerializeField, Min(0f)] private float _groundCheckDistance = 5f;

    protected override void PerformAttack()
    {
        if (_cannonballPrefab == null || _firePoint == null || Owner == null || !Owner.HasTarget)
            return;

        Vector3 targetPoint = Owner.Target.position;
        Vector3 velocity = CalculateArc(targetPoint);

        if (velocity.sqrMagnitude < 0.001f)
            return;

        Vector3 direction = velocity.normalized;
        float speed = velocity.magnitude;

        Projectile cannonball = Instantiate(_cannonballPrefab, _firePoint.position, Quaternion.LookRotation(direction));

        DamageInfo info = new DamageInfo
        {
            DamageAmount = Damage,
            DamageSource = Owner.gameObject,
            DamageDirection = direction,
            DamageType = DamageType.Physical
        };

        cannonball.Initialize(info, speed, _projectileRange, useGravity: true);

        GameObject marker = SpawnLandingMarker(targetPoint, velocity);
        HealingPool poolPrefab = _healingPoolPrefab;
        GameObject source = Owner.gameObject;

        // The attack or the orc can be gone by the time the cannonball lands, so everything the callback needs is copied.
        cannonball.OnImpact += (point, normal) =>
        {
            if (marker != null) Destroy(marker);
            SpawnHealingPool(poolPrefab, point, source);
        };
    }

    /// <summary>
    /// Shows the landing marker on the ground under the target point, and removes it once the cannonball should have
    /// landed, in case it's destroyed before it hits anything. Returns null without a marker prefab.
    /// </summary>
    private GameObject SpawnLandingMarker(Vector3 targetPoint, Vector3 velocity)
    {
        if (_landingMarkerPrefab == null)
            return null;

        GameObject marker = Instantiate(_landingMarkerPrefab, FindGround(targetPoint), Quaternion.identity);

        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 horizontalDistance = targetPoint - _firePoint.position;
        horizontalDistance.y = 0f;

        if (horizontalVelocity.sqrMagnitude > 0.001f)
            Destroy(marker, horizontalDistance.magnitude / horizontalVelocity.magnitude + 0.5f);

        return marker;
    }

    private void SpawnHealingPool(HealingPool poolPrefab, Vector3 point, GameObject source)
    {
        if (poolPrefab == null)
            return;

        HealingPool pool = Instantiate(poolPrefab, FindGround(point), Quaternion.identity);
        pool.Initialize(source);
    }

    /// <summary>The ground below a point, or the point itself when there's no ground within Ground Check Distance.</summary>
    private Vector3 FindGround(Vector3 point)
    {
        Vector3 origin = point + Vector3.up * 0.5f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, _groundCheckDistance + 0.5f, _groundLayers, QueryTriggerInteraction.Ignore))
            return hit.point;

        return point;
    }

    /// <summary>
    /// Returns the launch velocity that lands on the target at Launch Angle, raised when the target is high above the
    /// fire point. Returns zero when no arc can reach it.
    /// </summary>
    private Vector3 CalculateArc(Vector3 target)
    {
        Vector3 displacement = target - _firePoint.position;
        Vector3 horizontal = new Vector3(displacement.x, 0f, displacement.z);

        float distance = horizontal.magnitude;
        float height = displacement.y;
        float gravity = -Physics.gravity.y;

        if (distance < 0.001f || gravity <= 0f)
            return Vector3.zero;

        // Use the preferred angle unless the target is too high.
        float minimumAngle = Mathf.Atan2(height, distance) * Mathf.Rad2Deg + 10f;
        float angleDegrees = Mathf.Max(_launchAngle, minimumAngle);

        if (angleDegrees >= 85f)
            return Vector3.zero;

        float angle = angleDegrees * Mathf.Deg2Rad;

        // The speed needed to reach the target at this angle.
        float denominator = 2f * Mathf.Cos(angle) * Mathf.Cos(angle) * (distance * Mathf.Tan(angle) - height);

        if (denominator <= 0f)
            return Vector3.zero;

        float speed = Mathf.Sqrt(gravity * distance * distance / denominator);

        Vector3 direction = horizontal.normalized * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle);

        return direction.normalized * speed;
    }
}
