using UnityEngine;

[CreateAssetMenu(menuName = "Guns/Parts/Gun Barrel")]
public class GunBarrel : GunPart
{
    public FireMode FireMode;
    
    public override GunPartType PartType => GunPartType.Barrel;
}
