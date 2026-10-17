using UnityEngine;

[CreateAssetMenu(menuName = "Guns/Parts/Gun Underbarrel")]
public class GunUnderbarrel : GunPart
{
    public override GunPartType PartType => GunPartType.Underbarrel;
}
