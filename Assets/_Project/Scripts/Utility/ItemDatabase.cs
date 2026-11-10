using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

// One asset that lists every item in the game, grouped by category.
// Each category is a tab, and each item can be expanded to edit it in place,
// so you can find and change items without opening each asset.
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Database/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
  

    [TabGroup("Items", "Charms")]
    [Searchable, InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    [ListDrawerSettings(DraggableItems = true, ShowPaging = true, NumberOfItemsPerPage = 15, ShowIndexLabels = false)]
    [FormerlySerializedAs("AllCharms")]
    [SerializeField] private List<Charm> _charms = new();

    [TabGroup("Items", "Consumables")]
    [Searchable, InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    [ListDrawerSettings(DraggableItems = true, ShowPaging = true, NumberOfItemsPerPage = 15, ShowIndexLabels = false)]
    [FormerlySerializedAs("AllConsumables")]
    [SerializeField] private List<Consumable> _consumables = new();

    [TabGroup("Items", "Barrels")]
    [Searchable, InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    [ListDrawerSettings(DraggableItems = true, ShowPaging = true, NumberOfItemsPerPage = 15, ShowIndexLabels = false)]
    [FormerlySerializedAs("AllBarrels")]
    [SerializeField] private List<GunBarrel> _barrels = new();

    [TabGroup("Items", "Magazines")]
    [Searchable, InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    [ListDrawerSettings(DraggableItems = true, ShowPaging = true, NumberOfItemsPerPage = 15, ShowIndexLabels = false)]
    [FormerlySerializedAs("AllMagazines")]
    [SerializeField] private List<GunMagazine> _magazines = new();

    [TabGroup("Items", "Scopes")]
    [Searchable, InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    [ListDrawerSettings(DraggableItems = true, ShowPaging = true, NumberOfItemsPerPage = 15, ShowIndexLabels = false)]
    [FormerlySerializedAs("AllScope")]
    [SerializeField] private List<GunScope> _scopes = new();

    [TabGroup("Items", "Stocks")]
    [Searchable, InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    [ListDrawerSettings(DraggableItems = true, ShowPaging = true, NumberOfItemsPerPage = 15, ShowIndexLabels = false)]
    [FormerlySerializedAs("AllStocks")]
    [SerializeField] private List<GunStock> _stocks = new();

   

    public IReadOnlyList<Charm> Charms => _charms;
    public IReadOnlyList<Consumable> Consumables => _consumables;
    public IReadOnlyList<GunBarrel> Barrels => _barrels;
    public IReadOnlyList<GunMagazine> Magazines => _magazines;
    public IReadOnlyList<GunScope> Scopes => _scopes;
    public IReadOnlyList<GunStock> Stocks => _stocks;

    // Every gun part in one list
    public IEnumerable<GunPart> AllGunParts =>
        _barrels.Cast<GunPart>().Concat(_magazines).Concat(_scopes).Concat(_stocks);

    // Every item of any kind.
    public IEnumerable<IInventoryItem> AllItems =>
        _charms.Cast<IInventoryItem>().Concat(_consumables).Concat(AllGunParts);

    // Returns the item with this display name
    public IInventoryItem FindByName(string displayName)
    {
        return AllItems.FirstOrDefault(item => item != null && item.DisplayName == displayName);
    }

    // Returns every item of one rarity
    public IEnumerable<IInventoryItem> GetByRarity(ItemRarity rarity)
    {
        return AllItems.Where(item => item != null && item.Rarity == rarity);
    }

#if UNITY_EDITOR
  

    [PropertyOrder(-1)]
    [InfoBox("$Summary")]
    [Button("Find All Items In Project", ButtonSizes.Medium), DisableInPlayMode]
    private void FindAllItems()
    {
        _charms = FindAll<Charm>();
        _consumables = FindAll<Consumable>();
        _barrels = FindAll<GunBarrel>();
        _magazines = FindAll<GunMagazine>();
        _scopes = FindAll<GunScope>();
        _stocks = FindAll<GunStock>();

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    [PropertyOrder(-1)]
    [Button("Remove Empty Slots"), DisableInPlayMode]
    private void RemoveEmptySlots()
    {
        _charms.RemoveAll(item => item == null);
        _consumables.RemoveAll(item => item == null);
        _barrels.RemoveAll(item => item == null);
        _magazines.RemoveAll(item => item == null);
        _scopes.RemoveAll(item => item == null);
        _stocks.RemoveAll(item => item == null);

        EditorUtility.SetDirty(this);
    }

   
    private string Summary =>
        $"{_charms.Count} charms, {_consumables.Count} consumables, " +
        $"{_barrels.Count} barrels, {_magazines.Count} magazines, {_scopes.Count} scopes, {_stocks.Count} stocks";

    // Finds every asset of a type in the project, sorted by file name.
    private static List<T> FindAll<T>() where T : ScriptableObject
    {
        return AssetDatabase.FindAssets($"t:{typeof(T).Name}")
            .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(asset => asset != null)
            .OrderBy(asset => asset.name)
            .ToList();
    }
#endif
}