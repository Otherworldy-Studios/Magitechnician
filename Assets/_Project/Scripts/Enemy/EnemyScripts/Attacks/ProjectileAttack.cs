
using UnityEngine;

/// <summary>
/// Fires a projectile toward the player.
/// Wind-up and cooldown are handled by EnemyAttack.
/// </summary>
public class ProjectileAttack : EnemyAttack
{
    [SerializeField] private Projectile _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _projectileSpeed = 15f;

    // Additional field required by Projectile.Initialize().
    [SerializeField] private float _projectileRange = 20f;

    protected override void PerformAttack()
    {
        if (_projectilePrefab == null ||
            _firePoint == null ||
            Owner == null ||
            !Owner.HasTarget)
            return;

        Vector3 direction =
            Owner.Target.position - _firePoint.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Projectile projectile = Instantiate(
            _projectilePrefab,
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

        projectile.Initialize(
            info,
            _projectileSpeed,
            _projectileRange
        );
    }
}