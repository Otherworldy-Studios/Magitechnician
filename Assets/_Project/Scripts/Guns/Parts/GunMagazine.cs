using UnityEngine;

[CreateAssetMenu(menuName = "Guns/Parts/Gun Magazine")]
public class GunMagazine : GunPart
{
    [SerializeField] private int _magazineSize = 30;
    [SerializeField] private float _reloadSpeed = 2.0f;
    [SerializeField] private AmmoType _ammoType = AmmoType.Light;
    
    public override GunPartType PartType => GunPartType.Magazine;
}
