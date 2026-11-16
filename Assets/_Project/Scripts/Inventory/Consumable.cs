using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// A consumable, such as a healing potion: used once with the 1, 2, or 3 key during a run. Inventory.UseConsumable
/// applies its stat modifiers for its duration and heals by its instant heal amount. Consumables reset every run.
/// </summary>
[CreateAssetMenu(fileName = "Consumable", menuName = "InventoryItem/Consumable")]
public class Consumable : ScriptableObject, IInventoryItem
{
    [SerializeField] private string _displayName;
    [SerializeField, TextArea] private string _description;
    [SerializeField] private Sprite _icon;
    [Tooltip("Always Consumable. ItemSlotUI refuses the item if this is wrong.")]
    [SerializeField] private ItemCategory _category = ItemCategory.Consumable;
    [SerializeField, FormerlySerializedAs("rarity")] private ItemRarity _rarity;

    [Tooltip("What the consumable changes while active. Mark each one Permanent; Duration below decides how long they last.")]
    [SerializeField, FormerlySerializedAs("statModifiers")] private List<StatModifier> _modifiers = new();
    [Tooltip("Seconds the modifiers last. 0 keeps them for the rest of the run.")]
    [SerializeField, FormerlySerializedAs("duration")] private float _duration = 0f;
    [Tooltip("Health restored when used.")]
    [SerializeField, FormerlySerializedAs("instantHeal")] private int _instantHeal = 0;

    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public ItemCategory Category => _category;
    public ItemRarity Rarity => _rarity;
    /// <summary>What the consumable changes while active. Inventory.UseConsumable applies these.</summary>
    public List<StatModifier> Modifiers => _modifiers;
    /// <summary>Seconds the modifiers last. Inventory removes them when it ends. 0 keeps them for the rest of the run.</summary>
    public float Duration => _duration;
    /// <summary>Health restored when used.</summary>
    public int InstantHeal => _instantHeal;

    /// <summary>
    /// Old way to use a consumable: adds the modifiers to the first Stats found and heals. Unused, because
    /// Inventory.UseConsumable does this with the right Stats, a source, and the duration. Delete in cleanup.
    /// </summary>
    public void Use(GameObject player)
    {
        if (player.GetComponentInChildren<Stats>() != null)
        {
            Stats playerStats = player.GetComponentInChildren<Stats>();
            foreach (StatModifier statModifier in _modifiers)
            {
                playerStats.AddModifier(statModifier);
            }
        }

        if (_instantHeal > 0 && player.GetComponentInChildren<Health>())
        {
            Health playerHealth = player.GetComponentInChildren<Health>();
            playerHealth.RegenHealth(_instantHeal);
        }
    }
}
