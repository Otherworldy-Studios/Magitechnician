using UnityEngine;

/// <summary>
/// Anything the player can carry or be offered: charms, consumables, and gun parts. Code that only shows, stores,
/// or picks up an item uses this interface, so one ItemSlotUI, one ItemTooltip, and one reward screen work for all three.
/// Code that needs something specific, such as a charm's price, uses the item's own class.
/// </summary>
public interface IInventoryItem
{
    string DisplayName { get; }
    string Description { get; }
    Sprite Icon { get; }
    /// <summary>Which kind of item this is. ItemSlotUI only accepts items of its assigned category.</summary>
    ItemCategory Category { get; }
    /// <summary>INTEGRATION: new. ItemSlotUI colors its border by rarity, with colors from GlobalSettings.</summary>
    ItemRarity Rarity { get; }
}

/// <summary>The three kinds of item. Stored in assets as numbers, so renaming a value keeps the assets' values.</summary>
public enum ItemCategory
{
    Charm,
    Consumable,
    GunPart
}

/// <summary>How rare an item is. Each value has a border color in GlobalSettings.</summary>
public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic
}
