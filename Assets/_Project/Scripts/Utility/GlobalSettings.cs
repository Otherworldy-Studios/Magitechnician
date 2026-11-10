using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Project-wide settings in one asset, loaded from Resources/SO_GlobalSettings the first time Instance is read.
/// Holds the rarity colors.
/// </summary>
// INTEGRATION: new script. ItemSlotUI reads RarityColorSettings to color each slot's border by the item's rarity.
[CreateAssetMenu(fileName = "SO_GlobalSettings", menuName = "Settings/GlobalSettings")]
public class GlobalSettings : ScriptableObject
{
    private static GlobalSettings _instance; // Cached after the first load.

    /// <summary>The settings asset. Null if SO_GlobalSettings isn't in a Resources folder.</summary>
    public static GlobalSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GlobalSettings>("SO_GlobalSettings");
            }
            return _instance;
        }
    }

    [Tooltip("The border color for each item rarity.")]
    [SerializeField, FormerlySerializedAs("RarityColorSettings")] private RarityColors _rarityColors;

    /// <summary>The border color for each item rarity.</summary>
    public RarityColors RarityColorSettings => _rarityColors;
}

/// <summary>One color for each ItemRarity value.</summary>
[Serializable]
public struct RarityColors
{
    [SerializeField, FormerlySerializedAs("Common")] private Color _common;
    [SerializeField, FormerlySerializedAs("Uncommon")] private Color _uncommon;
    [SerializeField, FormerlySerializedAs("Rare")] private Color _rare;
    [SerializeField, FormerlySerializedAs("Epic")] private Color _epic;

    public Color Common => _common;
    public Color Uncommon => _uncommon;
    public Color Rare => _rare;
    public Color Epic => _epic;
}
