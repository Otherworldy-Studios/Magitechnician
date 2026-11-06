using UnityEngine;

[CreateAssetMenu(menuName = "Guns/Parts/Gun Magazine")]
public class GunMagazine : GunPart
{
    public DamageType DamageType;
    
    public override GunPartType PartType => GunPartType.Magazine;
}
