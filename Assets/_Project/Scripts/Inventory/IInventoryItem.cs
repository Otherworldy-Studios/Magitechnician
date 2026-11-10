using UnityEngine;

public interface IInventoryItem 
{
    public string DisplayName { get; }
    public string Description { get; }
    public Sprite Icon { get;}
    public ItemCategory Category { get; }
    public ItemRarity Rarity { get; }
}

public enum ItemCategory
{
    Charm,
    Consumable,
    Gunpart
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic
}
