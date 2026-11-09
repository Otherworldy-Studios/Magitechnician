using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Consumable", menuName = "InventoryItem/Consumable")]
public class Consumable : ScriptableObject, IInventoryItem
{
    [SerializeField] private string _displayName;
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;
    [SerializeField] private ItemCategory _category = ItemCategory.Consumable;
    [SerializeField] private ItemRarity rarity;
    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public ItemCategory Category => _category;
    public List<StatModifier> statModifiers = new();
    public float duration = 0f;
    public int instantHeal = 0;
    public ItemRarity Rarity => rarity;


    public void Use(GameObject player)
    {
        if(player.GetComponentInChildren<Stats>() != null)
        {
            Stats playerStats = player.GetComponentInChildren<Stats>();
            foreach(StatModifier statModifier in statModifiers)
            {
                playerStats.AddModifier(statModifier);
            }
        }

        if(instantHeal > 0 && player.GetComponentInChildren<Health>())
        {
            Health playerHealth = player.GetComponentInChildren<Health>();
            playerHealth.RegenHealth(instantHeal);
        }
    }


}
