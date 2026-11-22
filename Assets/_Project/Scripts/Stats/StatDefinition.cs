using UnityEngine;

/// <summary>
/// An asset that defines one stat, such as Stat_Speed or Stat_Damage. The asset itself is the stat's identity:
/// Stats keys its values by this asset, and scripts read a stat through a serialized StatDefinition field.
/// Final values are clamped between the minimum and maximum.
/// </summary>
[CreateAssetMenu(fileName = "StatDefinition", menuName = "Stats/StatDefinition")]
public class StatDefinition : ScriptableObject
{
    [Tooltip("The name shown in UI.")]
    [SerializeField] private string _displayName;
    [Tooltip("What the stat does. Shown in the tooltip when the player hovers over the stat, such as on the reward screen.")]
    [SerializeField, TextArea] private string _description;
    [SerializeField] private Sprite _icon;
    [Tooltip("On when a higher value is better, such as Damage. Off when lower is better, such as Spread. Decides whether a change shows green or red.")]
    [SerializeField] private bool _higherIsBetter = true;
    [Tooltip("The lowest final value. Stats clamps to this.")]
    [SerializeField] private float _minValue;
    [Tooltip("The highest final value. A maximum of 0 clamps every value to 0, so set it for every stat.")]
    [SerializeField] private float _maxValue;

    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public bool HigherIsBetter => _higherIsBetter;

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