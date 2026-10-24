using System;

// Holds the final stats of an assembled gun
[Serializable]
public struct GunStats
{
    public float range;
    public float damage;
    public float bulletSpeed;
    public float accuracy;
    public float spread;
    public float fireRate;
    public FireMode fireMode;
    public int magazineSize;
    public float reloadSpeed;
    public AmmoType ammoType;
    public float zoom;
}
