using UnityEngine;

/// <summary>A bullet's shape and collision settings. Projectile reads them every frame for its sphere cast.</summary>
[CreateAssetMenu(fileName = "ProjectileSettings", menuName = "Settings/Projectile")]
public class ProjectileSettings : ScriptableObject
{
    // INTEGRATION: Speed, MaxDistance, and Damage were removed. They're gun stats now (Stat_BulletSpeed, Stat_Range,
    // Stat_Damage), and Gun.TryFire passes them to each bullet through Projectile.Initialize.

    /// <summary>The sphere cast's radius in meters.</summary>
    [field: SerializeField] public float Radius { get; private set; } = 0.01f;
    /// <summary>Layers the bullet can hit. Needs Enemy, plus Default and Environment so bullets stop at walls.</summary>
    [field: SerializeField] public LayerMask BulletCollisionLayer { get; private set; }
    /// <summary>The bullet prefab. Unused: the Gun has its own projectile prefab field.</summary>
    [field: SerializeField] public GameObject Prefab { get; private set; }
}
