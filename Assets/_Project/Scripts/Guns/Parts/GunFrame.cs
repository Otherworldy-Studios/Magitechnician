using UnityEngine;

[CreateAssetMenu(menuName = "Guns/Parts/Gun Frame")]
public class GunFrame : GunPart
{
    [SerializeField] private float _fireRate = 30.0f;
    [SerializeField] private FireMode _fireMode = FireMode.Burst;
    
    public override GunPartType PartType => GunPartType.Frame;
}
