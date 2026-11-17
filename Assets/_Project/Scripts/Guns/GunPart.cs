using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Base asset for every gun part: its model, name, description, icon, rarity, and stat modifiers.
/// Implements IInventoryItem, so item slots, tooltips, and the reward screen can show any part.
/// When the Gun equips a part, PlayerRoot.ApplyModifiers sends each modifier to the gun's or the player's Stats.
/// </summary>
public abstract class GunPart : ScriptableObject, IInventoryItem
{
    [Tooltip("The model placed on the part's socket. A barrel's model needs a child named Muzzle.")]
    [SerializeField] private GameObject _partPrefab;
    [SerializeField] private string _partName;
    [SerializeField, TextArea] private string _description;
    [SerializeField] private Sprite _icon;
    [Tooltip("Sets the color of the item slot border.")]
    [SerializeField, FormerlySerializedAs("rarity")] private ItemRarity _rarity;

    [Tooltip("What the part changes, such as Damage Add 5. Mark each one Permanent.")]
    [SerializeField, FormerlySerializedAs("statModifiers")] private List<StatModifier> _modifiers = new();

    public GameObject PartPrefab => _partPrefab;
    public string PartName => _partName;
    /// <summary>Which socket this part goes on. Each part class returns its own type.</summary>
    public abstract GunPartType PartType { get; }
    /// <summary>The part's name. Same as PartName, so existing assets keep their names.</summary>
    public string DisplayName => _partName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public ItemCategory Category => ItemCategory.GunPart; // INTEGRATION: returns a fixed value, so the Inspector can't set the wrong category.
    // PlayerRoot.GetModifiers reads it when the Gun equips or removes the part.
    /// <summary>The part's stat modifiers.</summary>
    public List<StatModifier> Modifiers => _modifiers;
    public ItemRarity Rarity => _rarity; // INTEGRATION: new. ItemSlotUI colors its border by rarity.
}
