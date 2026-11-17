using UnityEngine;
using UnityEngine.Serialization;

/// <summary>A magazine part. Usually changes fire rate through its modifiers, and sets the damage type.</summary>
[CreateAssetMenu(menuName = "Guns/Parts/Gun Magazine")]
public class GunMagazine : GunPart
{
    [Tooltip("Used when the gun's StatBlock has no Stat_AmmoType. Kept until the Set modifier is confirmed.")]
    [SerializeField, FormerlySerializedAs("DamageType")] private DamageType _damageType;

    /// <summary>The damage type Gun.GetDamageType falls back to.</summary>
    public DamageType DamageType => _damageType;

    public override GunPartType PartType => GunPartType.Magazine;
}
