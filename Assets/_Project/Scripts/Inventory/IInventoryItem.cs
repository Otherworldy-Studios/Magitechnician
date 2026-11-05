using UnityEngine;

public interface IInventoryItem 
{
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public Sprite Icon { get; set; }
    public ItemCategory Category { get; set; }
}

public enum ItemCategory
{
    Charm,
    Consumable,
    Gunpart
}
