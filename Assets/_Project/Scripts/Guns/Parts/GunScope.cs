using UnityEngine;

[CreateAssetMenu(menuName = "Guns/Parts/Gun Scope")]
public class GunScope : GunPart
{
    [SerializeField] private float _zoom = 1.0f;
    
    public override GunPartType PartType => GunPartType.Scope;
}
