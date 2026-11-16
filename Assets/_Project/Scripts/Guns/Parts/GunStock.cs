using UnityEngine;

/// <summary>A stock part. Changes stats through its modifiers. Passive abilities come later through the item ability system.</summary>
[CreateAssetMenu(menuName = "Guns/Parts/Gun Stock")]
public class GunStock : GunPart
{
    // public StockPassive Passive;   // Replaced by the ability prefab. Delete in cleanup.

    public override GunPartType PartType => GunPartType.Stock;
}
