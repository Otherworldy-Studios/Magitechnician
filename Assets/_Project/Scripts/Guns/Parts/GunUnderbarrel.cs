using UnityEngine;

[CreateAssetMenu(menuName = "Guns/Parts/Gun Underbarrel")]
public class GunUnderbarrel : GunPart
{
    // public SecondaryFire SecondaryFire;
    
    public override GunPartType PartType => GunPartType.Underbarrel;
}
