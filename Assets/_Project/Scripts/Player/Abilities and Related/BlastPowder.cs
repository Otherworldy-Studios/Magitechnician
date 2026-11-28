
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// While this consumable is active, hitting an enemy
/// creates an explosion that damages nearby enemies.
/// </summary>
public class BlastPowder : ItemAbility
{
    [Header("Blast Powder Settings")]

    [SerializeField, Min(0.1f)]
    private float _explosionRadius = 1.5f;

    // Temporary test value. Confirm final damage with the team.
    [SerializeField, Min(1)]
    private int _explosionDamage = 10;

    [Header("Optional Visual Effect")]

    [SerializeField]
    private GameObject _explosionEffectPrefab;

    [SerializeField, Min(0.1f)]
    private float _effectLifetime = 2f;

    // Prevents damaging the same enemy multiple times
    // if it has several colliders.
    private readonly HashSet<EnemyBase> _damagedEnemies = new();

    private void OnEnable()
    {
        if (PlayerGun == null) return;

        PlayerGun.OnDamageDealt += HandleDamageDealt;
    }

    private void OnDisable()
    {
        if (PlayerGun == null) return;

        PlayerGun.OnDamageDealt -= HandleDamageDealt;
    }

    private void HandleDamageDealt(
        GameObject target,
        DamageInfo damageInfo)
    {
        if (target == null) return;

        // Only activate when a bullet hits an enemy.
        EnemyBase hitEnemy =
            target.GetComponentInParent<EnemyBase>();

        if (hitEnemy == null)
            hitEnemy = target.GetComponentInChildren<EnemyBase>();

        if (hitEnemy == null) return;

        // Find the center of the enemy that was hit.
        Collider enemyCollider =
            hitEnemy.GetComponentInChildren<Collider>();

        Vector3 explosionCenter = enemyCollider != null
            ? enemyCollider.bounds.center
            : hitEnemy.transform.position;

        // Find everything within the explosion radius.
        Collider[] nearbyColliders =
            Physics.OverlapSphere(
                explosionCenter,
                _explosionRadius
            );

        _damagedEnemies.Clear();

        foreach (Collider nearbyCollider in nearbyColliders)
        {
            EnemyBase nearbyEnemy =
                nearbyCollider.GetComponentInParent<EnemyBase>();

            // Only damage enemies.
            if (nearbyEnemy == null) continue;

            // Don't damage an enemy more than once
            // during the same explosion.
            if (!_damagedEnemies.Add(nearbyEnemy))
                continue;

            Health enemyHealth = nearbyEnemy.Health;

            if (enemyHealth == null ||
                enemyHealth.CurrentHealth <= 0)
                continue;

            Vector3 direction =
                (nearbyEnemy.transform.position -
                 explosionCenter).normalized;

            DamageInfo explosionDamage = new DamageInfo
            {
                DamageAmount = _explosionDamage,
                DamageSource = Player != null
                    ? Player.gameObject
                    : damageInfo.DamageSource,
                DamageDirection = direction,
                DamageType = damageInfo.DamageType
            };

            // Respect the enemy's damage receiver if present.
            DamageReceiver receiver =
                nearbyEnemy.GetComponentInChildren<DamageReceiver>();

            if (receiver != null)
            {
                receiver.TakeDamage(explosionDamage);
            }
            else
            {
                enemyHealth.TakeDamage(explosionDamage);
            }
        }

        // Optional explosion particle effect.
        if (_explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                _explosionEffectPrefab,
                explosionCenter,
                Quaternion.identity
            );

            Destroy(effect, _effectLifetime);
        }

        // Tell the ability system that Blast Powder activated.
        ReportActivated();
    }
}
