using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Shield values: max shield, regen delays, regen rate, and damage reduction. ShieldSystem reads each value from a stat
/// and uses this asset only as the fallback when that stat isn't assigned. Delete once every prefab has its shield stats.
/// </summary>
[CreateAssetMenu(fileName = "ShieldSettings", menuName = "GameData/ShieldConfig")]
public class ShieldSettings : ScriptableObject
{
    [SerializeField, FormerlySerializedAs("maxShield")] private float _maxShield = 50f;
    [Tooltip("Seconds without damage before the shield starts to regenerate.")]
    [SerializeField, FormerlySerializedAs("regenDelay")] private float _regenDelay = 3f;
    [Tooltip("Seconds before regeneration starts after the shield breaks.")]
    [SerializeField, FormerlySerializedAs("brokenRegenDelay")] private float _brokenRegenDelay = 5f;
    [Tooltip("Shield points regained per second.")]
    [SerializeField, FormerlySerializedAs("regenRate")] private float _regenRate = 10f;
    [Tooltip("Percentage of incoming damage the shield ignores.")]
    [SerializeField, FormerlySerializedAs("damageReductionPercent"), Range(0f, 100f)] private float _damageReductionPercent = 25f;

    public float MaxShield => _maxShield;
    public float RegenDelay => _regenDelay;
    public float BrokenRegenDelay => _brokenRegenDelay;
    public float RegenRate => _regenRate;
    public float DamageReductionPercent => _damageReductionPercent;
}
