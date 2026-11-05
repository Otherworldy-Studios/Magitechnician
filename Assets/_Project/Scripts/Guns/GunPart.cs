using System.Collections.Generic;
using UnityEngine;

// Base class for all gun parts
public abstract class GunPart : ScriptableObject , IInventoryItem
{
    [SerializeField] private GameObject _partPrefab;
    [SerializeField] private string _partName;
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;

    private ItemCategory _category = ItemCategory.Gunpart;

    public GameObject PartPrefab => _partPrefab;
    public string PartName => _partName;
    public abstract GunPartType PartType { get; }
    public string DisplayName => _partName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public ItemCategory Category => _category;
    public List<StatModifier> statModifiers = new();
}
