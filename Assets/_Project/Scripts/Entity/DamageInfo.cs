using UnityEngine;

/// <summary>
/// Everything about one hit, passed to IDamageable.TakeDamage: how much, who caused it, which way it pushed,
/// and its type. Damage-over-time ticks pass the original source and type too.
/// </summary>
public struct DamageInfo
{
    // The properties have public setters, so object initializers such as new DamageInfo { DamageAmount = 10 } still work.
    /// <summary>Damage dealt, in whole points.</summary>
    public int DamageAmount { get; set; }
    /// <summary>The object that caused the damage. Projectiles skip colliders under it, so the shooter can't hit itself.</summary>
    public GameObject DamageSource { get; set; }
    /// <summary>The direction the hit traveled.</summary>
    public Vector3 DamageDirection { get; set; }
    /// <summary>The element of the hit. The Gun takes it from Stat_AmmoType or the magazine.</summary>
    public DamageType DamageType { get; set; }
}

/// <summary>The element of a hit. Only Physical has behavior so far; DamageReceiver has empty cases for the rest.</summary>
public enum DamageType { Physical, Fire, Ice, Electric, Poison }
