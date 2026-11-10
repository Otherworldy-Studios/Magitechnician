using UnityEngine;

[CreateAssetMenu(menuName = "Guns/Parts/Gun Scope")]
public class GunScope : GunPart
{
    public float Zoom;
    
    public override GunPartType PartType => GunPartType.Scope;
}
