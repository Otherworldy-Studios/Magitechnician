using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
[CreateAssetMenu(fileName = "Charm", menuName = "InventoryItem/Charm")]
public class Charm : ScriptableObject, IInventoryItem
{
    [SerializeField] private string _displayName;
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;
    [SerializeField, ReadOnly] private ItemCategory _category = ItemCategory.Charm;
    [SerializeField] private ItemRarity rarity;
    public string DisplayName => _displayName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public ItemCategory Category => _category;

    public ItemRarity Rarity => rarity;
    
    public int Price = 0;
    public List<StatModifier> statModifiers = new();
}
