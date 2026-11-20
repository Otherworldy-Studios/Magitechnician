using UnityEngine;

/// <summary>
/// Launches a cannonball toward the player using
/// a calculated ballistic launch direction.
/// </summary>
public class ArcProjectileAttack : EnemyAttack
{
    [SerializeField] private Projectile _cannonballPrefab;
    [SerializeField] private Transform _firePoint;

    // Required by projectile initialization and arc calculation.
    [SerializeField, Range(10f, 85f)] private float _launchAngle = 55f;
    [SerializeField] private float _projectileRange = 100f;

    protected override void PerformAttack()
    {
        if (_cannonballPrefab == null ||
            _firePoint == null ||
            Owner == null ||
            !Owner.HasTarget)
            return;

        Vector3 velocity = CalculateArc(Owner.Target.position);

        if (velocity.sqrMagnitude < 0.001f)
            return;

        Vector3 direction = velocity.normalized;
        float speed = velocity.magnitude;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Projectile cannonball = Instantiate(
            _cannonballPrefab,
            _firePoint.position,
            Quaternion.LookRotation(direction)
        );

        DamageInfo info = new DamageInfo
        {
            DamageAmount = Mathf.RoundToInt(Owner.AttackDamage),
            DamageSource = Owner.gameObject,
            DamageDirection = direction.normalized,
            DamageType = DamageType.Physical
        };

        cannonball.Initialize(
            info,
            speed,
            _projectileRange,
            useGravity: true
        );
    }

    private Vector3 CalculateArc(Vector3 target)
    {
        Vector3 displacement = target - _firePoint.position;

        Vector3 horizontal = new Vector3(
            displacement.x, 0f, displacement.z
        );

        float distance = horizontal.magnitude;
        float height = displacement.y;
        float gravity = -Physics.gravity.y;

        if (distance < 0.001f || gravity <= 0f)
            return Vector3.zero;

        // Use the preferred angle unless the target is too high.
        float minimumAngle =
            Mathf.Atan2(height, distance) * Mathf.Rad2Deg + 10f;

        float angleDegrees = Mathf.Max(_launchAngle, minimumAngle);

        if (angleDegrees >= 85f)
            return Vector3.zero;

        float angle = angleDegrees * Mathf.Deg2Rad;

        // Calculate the speed needed to reach the target.
        float denominator = 2f * Mathf.Cos(angle) *
                            Mathf.Cos(angle) *
                            (distance * Mathf.Tan(angle) - height);

        if (denominator <= 0f)
            return Vector3.zero;

        float speed = Mathf.Sqrt(
            gravity * distance * distance / denominator
        );

        // Combine horizontal and vertical launch components.
        Vector3 direction =
            horizontal.normalized * Mathf.Cos(angle) +
            Vector3.up * Mathf.Sin(angle);

        return direction.normalized * speed;
    }
}