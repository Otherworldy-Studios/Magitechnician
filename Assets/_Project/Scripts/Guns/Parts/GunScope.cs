using UnityEngine;

[CreateAssetMenu(menuName = "Guns/Parts/Gun Scope")]
public class GunScope : GunPart
{
    public override GunPartType PartType => GunPartType.Scope;
}
