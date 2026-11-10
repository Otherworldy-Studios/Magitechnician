using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileSettings", menuName = "Settings/Projectile")]
public class ProjectileSettings : ScriptableObject
{
    // removed Speed, MaxDistance, and Damage isnce They're gun stats now 
    // and Gun passes them to each bullet in Projectile.Initialize.
    [field: SerializeField] public float Radius { get; private set; } = 0.01f;
    [field: SerializeField] public LayerMask BulletCollisionLayer { get; private set; }
    [field: SerializeField] public GameObject Prefab { get; private set; }
}