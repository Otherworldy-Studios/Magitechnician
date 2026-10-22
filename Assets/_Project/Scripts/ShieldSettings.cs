using UnityEngine;

[CreateAssetMenu(fileName = "ShieldSettings", menuName = "GameData/ShieldConfig")]
public class ShieldSettings : ScriptableObject
{
    public float maxShield = 50f;
    public float regenDelay = 3f;
    public float brokenRegenDelay = 5f;
    public float regenRate = 10f;
    [Range(0f, 100f)] public float damageReductionPercent = 25f;
}
