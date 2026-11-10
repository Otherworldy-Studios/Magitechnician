using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// One asset that lists every item in the game, grouped by category. Each category is a tab, and each item
/// can be expanded to edit it in place, so items can be found and changed without opening each asset.
/// An editor tool: nothing reads it during play.
/// </summary>
// INTEGRATION: new script, made by Lee. The Find All Items In Project button fills the lists from every item asset,
// including the ones the JSON importer creates.
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Database/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    // One list per item type. Odin draws each as a searchable tab with every item expandable in place.
    // FormerlySerializedAs keeps the values from when the lists were named AllCharms, AllConsumables, and so on.

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

    /// <summary>Every barrel, magazine, scope, and stock in one sequence.</summary>
    public IEnumerable<GunPart> AllGunParts =>
        _barrels.Cast<GunPart>().Concat(_magazines).Concat(_scopes).Concat(_stocks);

    /// <summary>Every charm, consumable, and gun part in one sequence.</summary>
    public IEnumerable<IInventoryItem> AllItems =>
        _charms.Cast<IInventoryItem>().Concat(_consumables).Concat(AllGunParts);

    /// <summary>Returns the first item with this display name, or null if none has it.</summary>
    public IInventoryItem FindByName(string displayName)
    {
        return AllItems.FirstOrDefault(item => item != null && item.DisplayName == displayName);
    }

    /// <summary>Returns every item of one rarity.</summary>
    public IEnumerable<IInventoryItem> GetByRarity(ItemRarity rarity)
    {
        return AllItems.Where(item => item != null && item.Rarity == rarity);
    }

#if UNITY_EDITOR
    // Editor-only buttons. They use AssetDatabase, which doesn't exist in a build.

    /// <summary>Fills every list with every asset of its type in the project, sorted by file name, and saves.</summary>
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

    /// <summary>Removes empty entries, such as items that were deleted.</summary>
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

    // The item counts shown in the info box above the buttons.
    private string Summary =>
        $"{_charms.Count} charms, {_consumables.Count} consumables, " +
        $"{_barrels.Count} barrels, {_magazines.Count} magazines, {_scopes.Count} scopes, {_stocks.Count} stocks";

    /// <summary>Finds every asset of a type in the project, sorted by file name.</summary>
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