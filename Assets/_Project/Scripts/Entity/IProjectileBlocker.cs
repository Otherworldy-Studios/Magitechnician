using UnityEngine;

/// <summary>
/// Something that stops projectiles without taking damage, such as the Dark Knight's shield. Projectile checks the
/// collider it hit for this before looking for an IDamageable, and only on that collider's own object.
/// </summary>
public interface IProjectileBlocker
{
    /// <summary>Runs when a projectile hits this blocker, just before the projectile is destroyed.</summary>
    /// <param name="point">Where the projectile hit.</param>
    /// <param name="direction">The direction the projectile was moving.</param>
    void BlockProjectile(Vector3 point, Vector3 direction);
}
