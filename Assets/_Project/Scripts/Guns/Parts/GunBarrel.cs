using UnityEngine;

[CreateAssetMenu(menuName = "Guns/Parts/Gun Barrel")]
public class GunBarrel : GunPart
{
    [SerializeField] private float _range = 100.0f;
    
    public override GunPartType PartType => GunPartType.Barrel;
}
