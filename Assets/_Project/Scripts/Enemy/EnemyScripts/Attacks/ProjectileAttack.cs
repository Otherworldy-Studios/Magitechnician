using UnityEngine;

/// <summary>
/// Fires a projectile at the center of the player's body. Wind-up and cooldown are handled by EnemyAttack.
/// The Corrupted Insectoid's spike uses this.
/// </summary>
public class ProjectileAttack : EnemyAttack
{
    [SerializeField] private Projectile _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _projectileSpeed = 15f;

    [Tooltip("Meters the projectile travels before it's destroyed.")]
    [SerializeField] private float _projectileRange = 20f;

    // Cached, so the GetComponentInChildren search in GetAimPoint only runs when the target changes.
    private Transform _cachedTarget;
    private CharacterController _targetController;

    protected override void PerformAttack()
    {
        base.PerformAttack();
        if (_projectilePrefab == null || _firePoint == null || Owner == null || !Owner.HasTarget)
            return;

        Vector3 direction = GetAimPoint() - _firePoint.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Projectile projectile = Instantiate(_projectilePrefab, _firePoint.position, Quaternion.LookRotation(direction));

        DamageInfo info = new DamageInfo
        {
            DamageAmount = Damage,
            DamageSource = Owner.gameObject,
            DamageDirection = direction.normalized,
            DamageType = DamageType.Physical
        };

        projectile.Initialize(info, _projectileSpeed, _projectileRange);
    }

    /// <summary>
    /// The center of the target's CharacterController, the same point LineOfSight checks. Falls back to the target's
    /// position when there's no CharacterController. Doesn't lead a moving target, so the player can dodge by moving.
    /// </summary>
    private Vector3 GetAimPoint()
    {
        Transform target = Owner.Target;

        if (target != _cachedTarget)
        {
            _cachedTarget = target;
            _targetController = target.GetComponentInChildren<CharacterController>();
        }

        return _targetController != null ? _targetController.bounds.center : target.position;
    }
}