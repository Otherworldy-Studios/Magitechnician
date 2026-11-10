using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// A charm: a lasting item bought in the shop with crystals and equipped at the charm station.
/// Equipping applies its stat modifiers through Inventory.EquipCharm. Owned and equipped charms carry
/// between runs through PersistentProgress.
/// </summary>
[CreateAssetMenu(fileName = "Charm", menuName = "InventoryItem/Charm")]
public class Charm : ScriptableObject, IInventoryItem
{
    [SerializeField] private string _displayName;
    [SerializeField, TextArea] private string _description;
    [SerializeField] private Sprite _icon;
    [Tooltip("Always Charm. Shown read-only so it can't be set wrong.")]
    [SerializeField, ReadOnly] private ItemCategory _category = ItemCategory.Charm; // INTEGRATION: made read-only in the Inspector.
    [SerializeField, FormerlySerializedAs("rarity")] private ItemRarity _rarity;
    [Tooltip("Crystals it costs in the shop.")]
    [SerializeField, FormerlySerializedAs("Price")] private int _price = 0;
    [Tooltip("What the charm changes while equipped. Mark each one Permanent.")]
    [SerializeField, FormerlySerializedAs("statModifiers")] private List<StatModifier> _modifiers = new();

    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public ItemCategory Category => _category;
    public ItemRarity Rarity => _rarity;
    /// <summary>Crystals it costs in the shop. Shop.TryBuy reads it.</summary>
    public int Price => _price;
    /// <summary>What the charm changes while equipped. Inventory.EquipCharm applies these.</summary>
    public List<StatModifier> Modifiers => _modifiers;
}
