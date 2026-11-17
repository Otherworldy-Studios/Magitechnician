/// <summary>
/// Anything that can take damage. Attacks only call TakeDamage on this interface, so they work on the player,
/// enemies, and anything else that implements it. DamageReceiver is the implementation on characters.
/// Projectile and EnemyMeleeHitbox find it with GetComponentInParent on the collider they hit.
/// </summary>
public interface IDamageable
{
    /// <summary>Applies one hit.</summary>
    void TakeDamage(DamageInfo damageInfo);
}
