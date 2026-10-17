using UnityEngine;

[CreateAssetMenu(menuName = "Guns/Parts/Gun Stock")]
public class GunStock : GunPart
{
    public override GunPartType PartType => GunPartType.Stock;
}
