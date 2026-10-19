using UnityEngine;

public struct DamageInfo {
    public float DamageAmount;
    public GameObject DamageSource;
    public Vector3 DamageDirection;
    public DamageType DamageType;
}

public enum DamageType {Physical, Fire, Ice, Electric, Poison}
