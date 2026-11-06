using UnityEngine;

[CreateAssetMenu(menuName = "Guns/Parts/Gun Stock")]
public class GunStock : GunPart
{
    // public StockPassive Passive;
    
    public override GunPartType PartType => GunPartType.Stock;
}
