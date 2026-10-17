using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileSettings", menuName = "Settings/Projectile")]
public class ProjectileSettings : ScriptableObject
{
    [field: SerializeField] public float Speed { get; private set; } = 40f;
    [field: SerializeField] public float Radius { get; private set; } = 0.01f;
    [field: SerializeField] public float MaxDistance { get; private set; } = 100f;
    [field: SerializeField] public LayerMask BulletCollisionLayer { get; private set; }
    [field: SerializeField] public float Damage { get; private set; } = 10f;
    [field: SerializeField] public GameObject Prefab { get; private set; }
}
