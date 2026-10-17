using UnityEngine;

[CreateAssetMenu(fileName = "GunStats", menuName = "Scriptable Objects/GunStats")]
public class GunStats : ScriptableObject
{
    public Stat[] stats;



    /*public GunStats(float fireRate, string fireType, int burstCount, float burstDelay, float damage, float range, float bulletSpeed, float spread, float recoilKick, float recoilSidewaysAmount, float recoilRecoverySpeed, int magazineSize, float reloadSpeed, string ammoType)
    {
        this.fireRate = fireRate;
        this.fireType =  fireType;
        this.burstCount = burstCount;
        this.burstDelay = burstDelay;
        this.damage = damage;
        this.range = range;
        this.bulletSpeed = bulletSpeed;
        this.spread = spread;
        this.recoilKick = recoilKick;
        this.recoilSidewaysAmount = recoilSidewaysAmount;
        this.recoilRecoverySpeed = recoilRecoverySpeed;
        this.magazineSize = magazineSize;
        this.reloadSpeed = reloadSpeed;
        this.ammoType = ammoType;
    }
    */

    
}

public enum fireType {Burst, Auto, SemiAuto}
public enum ammoType { }
