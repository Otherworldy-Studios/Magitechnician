using UnityEngine;

[CreateAssetMenu(fileName = "StatDefinition", menuName = "StatsRefactor/StatDefinition")]

// This class is a ScriptableObject that defines a stat, including its display name, icon, and min/max values.
public class StatDefinition : ScriptableObject
{
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _icon;
    [SerializeField] private float _minValue;
    [SerializeField] private float _maxValue;

    public string DisplayName => _displayName;
    public Sprite Icon => _icon;

    // This method clamps a given value between the min and max values defined for this stat.
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
