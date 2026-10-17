using UnityEngine;

[CreateAssetMenu(fileName = "Stat", menuName = "Scriptable Objects/Stat")]
public class Stat : ScriptableObject
{
    public float value;
    public GunStat gunStatName;

    public float AddTo(float value)
    {
        this.value += value;
        return this.value;
    }

    public float MultiplyBy(float value)
    {
        this.value *= value;
        return this.value;
    }

    public float GetStatValue()
    {
        return this.value;
    }
}

public enum GunStat { FireRate, FireType, BurstCount, BurstDelay, Damage, Range, BulletSpeed, Spread, MagazineSize, ReloadSpeed, AmmoType }
