using UnityEngine;

/// <summary>
/// An asset that defines one stat, such as Stat_Speed or Stat_Damage. The asset itself is the stat's identity:
/// Stats keys its values by this asset, and scripts read a stat through a serialized StatDefinition field.
/// Final values are clamped between the minimum and maximum.
/// </summary>
[CreateAssetMenu(fileName = "StatDefinition", menuName = "Stats/StatDefinition")] // INTEGRATION: menu moved from StatsRefactor to Stats.
public class StatDefinition : ScriptableObject
{
    [Tooltip("The name shown in UI.")]
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;
    [Tooltip("The lowest final value. Stats clamps to this.")]
    [SerializeField] private float _minValue;
    [Tooltip("The highest final value. A maximum of 0 clamps every value to 0, so set it for every stat.")]
    [SerializeField] private float _maxValue;

    public string DisplayName => _displayName;
    public Sprite Icon => _icon;

    /// <summary>Returns the value limited to this stat's minimum and maximum.</summary>
    /// <param name="value">A calculated final value.</param>
    public float Clamp(float value)
    {
        if (value < _minValue)
        {
            return _minValue;
        }
        else if (value > _maxValue)
        {
            return _maxValue;
        }
        else
        {
            return value;
        }
    }
}
