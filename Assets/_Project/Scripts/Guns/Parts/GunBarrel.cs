using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// A barrel part. Usually changes damage, range, bullet speed, and spread through its modifiers, and sets the fire mode.
/// Its model needs a child named Muzzle, which Gun uses as the bullet's start point.
/// </summary>
[CreateAssetMenu(menuName = "Guns/Parts/Gun Barrel")]
public class GunBarrel : GunPart
{
    [Tooltip("Used when the gun's StatBlock has no Stat_FireType. Kept until the Set modifier is confirmed.")]
    [SerializeField, FormerlySerializedAs("FireMode")] private FireMode _fireMode;

    [SerializeField, Min(1)] private int _burstCount = 3;
    [SerializeField, Min(0.01f)] private float _burstDelay = 0.1f;
    
    /// <summary>The fire mode Gun.RefreshFireMode falls back to.</summary>
    public FireMode FireMode => _fireMode;

    public int BurstCount => _burstCount;
    public float BurstDelay => _burstDelay;
    
    public override GunPartType PartType => GunPartType.Barrel;
}
