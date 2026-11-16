using UnityEngine;

/// <summary>An underbarrel part. Secondary fire comes later through the item ability system, so the demo has none.</summary>
[CreateAssetMenu(menuName = "Guns/Parts/Gun Underbarrel")]
public class GunUnderbarrel : GunPart
{
    // public SecondaryFire SecondaryFire;   // Replaced by the ability prefab. Delete in cleanup.

    public override GunPartType PartType => GunPartType.Underbarrel;
}
