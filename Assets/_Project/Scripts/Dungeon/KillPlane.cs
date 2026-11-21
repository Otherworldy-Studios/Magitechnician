using UnityEngine;

/// <summary>
/// A trigger below the dungeon that kills whatever falls into it. Enemies die through their Health, so EnemyBase.Die
/// still removes them from WaveManager and the wave can finish. Needs a trigger collider and a kinematic Rigidbody on
/// this object, because NavMeshAgent enemies don't have a Rigidbody of their own.
/// </summary>
public class KillPlane : MonoBehaviour
{
    private readonly DamageInfo _damageInfo = new DamageInfo
    {
        DamageAmount = int.MaxValue,
        DamageDirection = Vector3.zero,
        DamageSource = null,
        DamageType = DamageType.Physical
    };

    private void OnTriggerEnter(Collider other)
    {
        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            if (enemy.Health != null) enemy.Health.TakeDamage(_damageInfo);
            return;
        }

        PlayerRoot player = other.GetComponentInParent<PlayerRoot>();
        if (player != null && player.PlayerHealth != null)
        {
            player.PlayerHealth.TakeDamage(_damageInfo);
        }
    }
}