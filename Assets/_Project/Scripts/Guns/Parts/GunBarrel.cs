using UnityEngine;

[CreateAssetMenu(menuName = "Guns/Parts/Gun Barrel")]
public class GunBarrel : GunPart
{
    [SerializeField] private float _range = 100.0f;
    [SerializeField] private int _damage = 5;
    [SerializeField] private float _bulletSpeed = 200.0f;
    [SerializeField] private float _accuracy = 85.0f;
    [SerializeField] private float _spread = 1.0f;
    
    
    public override GunPartType PartType => GunPartType.Barrel;
}
