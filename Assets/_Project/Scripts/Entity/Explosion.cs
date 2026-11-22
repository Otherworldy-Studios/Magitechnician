using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Deals one hit to everything that can take damage inside a sphere. Each IDamageable is hit once, however many of its
/// colliders are inside. The Corrupted Insectoid's pustule uses this.
/// </summary>
public static class Explosion
{
    /// <summary>
    /// Damages every IDamageable within the radius once, the player and enemies included. Each hit's direction points
    /// away from the center. Returns how many were hit.
    /// </summary>
    /// <param name="center">The explosion's center.</param>
    /// <param name="radius">Meters from the center that are hit.</param>
    /// <param name="damageInfo">The hit. Its DamageSource should be the object that exploded.</param>
    /// <param name="layers">Layers that can be hit.</param>
    /// <param name="ignore">Colliders under this transform are skipped, such as the exploding enemy. Can be null.</param>
    public static int Explode(Vector3 center, float radius, DamageInfo damageInfo, LayerMask layers, Transform ignore = null)
    {
        // A new array each time, because a hit can set off another explosion, such as a second pustule, mid-loop.
        Collider[] hits = Physics.OverlapSphere(center, radius, layers, QueryTriggerInteraction.Collide);
        HashSet<IDamageable> damaged = new HashSet<IDamageable>();

        foreach (Collider hit in hits)
        {
            if (hit == null || (ignore != null && hit.transform.IsChildOf(ignore)))
                continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null || !damaged.Add(damageable))
                continue;

            Vector3 away = hit.bounds.center - center;
            damageInfo.DamageDirection = away.sqrMagnitude > 0.0001f ? away.normalized : Vector3.up;

            damageable.TakeDamage(damageInfo);
        }

        return damaged.Count;
    }
}
