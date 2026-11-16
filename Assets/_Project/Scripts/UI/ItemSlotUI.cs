using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// One item slot in the UI. Shows an item's icon with a border in its rarity color, highlights when selected, and shows
/// the tooltip on hover. Only takes items of its assigned category. ItemGridUI and ConsumableSlotsUI fill these slots.
/// </summary>
// INTEGRATION: rebuilt. Before, it only set an icon and a name label and could never be clicked or hovered. It now
// implements the pointer interfaces, stores its item, and fires OnClicked and OnCleared, which ItemGridUI (new) uses.
public class ItemSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Only items of this category can go in this slot.")]
    [SerializeField] private ItemCategory _assignedCategory;

    // INTEGRATION: replaces the unserialized _icon and _name fields, which were never assigned.
    [SerializeField] private Image _iconImage;
    [SerializeField] private GameObject _selectedHighlight; // Turned on while the slot is selected.

    // Colored from GlobalSettings.RarityColorSettings.
    [SerializeField, FormerlySerializedAs("rarityBorder")] private Image _rarityBorder;

    [Tooltip("Found by ItemGridUI through SetTooltip if empty.")]
    [SerializeField] private ItemTooltip _tooltip;

    [SerializeField] private SlotType _slotType = SlotType.Inventory; // Which kind of screen this slot is on. Not read yet.

    // INTEGRATION: removed maxCount and currentCount, which nothing read.

    /// <summary>True while the slot is selected. Set by ItemGridUI.</summary>
    [ShowInInspector, ReadOnly] public bool Selected { get; private set; }

    private IInventoryItem _item;    // The item shown, or null.
    private Color _emptyBorderColor; // INTEGRATION: new. The border color set in the prefab, used when empty.

    /// <summary>The item in this slot, or null.</summary>
    public IInventoryItem Item => _item;
    /// <summary>True when the slot has no item.</summary>
    public bool IsEmpty => _item == null;

    /// <summary>Fires when the slot is clicked, with this slot. ItemGridUI selects it.</summary>
    public event Action<ItemSlotUI> OnClicked;

    /// <summary>Fires when a slot that had an item is emptied, with this slot.</summary>
    // INTEGRATION: new. Lets ItemGridUI deselect a slot that was emptied.
    public event Action<ItemSlotUI> OnCleared;

    /// <summary>Remembers the empty border color and starts unselected.</summary>
    private void Awake()
    {
        if (_rarityBorder != null) _emptyBorderColor = _rarityBorder.color;
        if (_selectedHighlight != null) _selectedHighlight.SetActive(false);
    }

    /// <summary>Gives the slot a tooltip, unless one is already assigned. ItemGridUI calls this for every slot.</summary>
    public void SetTooltip(ItemTooltip tooltip)
    {
        if (_tooltip == null) _tooltip = tooltip;
    }

    /// <summary>
    /// Shows an item. Null empties the slot. An item of the wrong category is refused with a warning, and the slot empties.
    /// </summary>
    public void SetItem(IInventoryItem item)
    {
        if (item == null)
        {
            Clear();
            return;
        }

        if (item.Category != _assignedCategory)
        {
            Debug.LogWarning($"{name}: {item.DisplayName} is a {item.Category}, but this slot only takes {_assignedCategory}.");
            Clear(); // INTEGRATION: was only a return, which left the old item showing.
            return;
        }

        _item = item;
        _iconImage.sprite = item.Icon;
        _iconImage.enabled = item.Icon != null; // Hides the image instead of showing a white box.
        SetBorderColor(item.Rarity);
    }

    /// <summary>
    /// Colors the border by rarity from GlobalSettings. Skips the color if the border or GlobalSettings is missing,
    /// instead of throwing.
    /// </summary>
    private void SetBorderColor(ItemRarity rarity)
    {
        if (_rarityBorder == null) return;

        if (GlobalSettings.Instance == null)
        {
            Debug.LogWarning($"{name}: no GlobalSettings found in Resources, so the rarity border isn't colored.", this);
            return;
        }

        var colors = GlobalSettings.Instance.RarityColorSettings;
        _rarityBorder.color = rarity switch
        {
            ItemRarity.Common => colors.Common,
            ItemRarity.Uncommon => colors.Uncommon,
            ItemRarity.Rare => colors.Rare,
            ItemRarity.Epic => colors.Epic,
            _ => _emptyBorderColor
        };
    }

    /// <summary>Empties the slot, resets the border, hides the tooltip, and fires OnCleared if there was an item.</summary>
    public void Clear()
    {
        bool hadItem = _item != null;

        _item = null;
        _iconImage.sprite = null;
        _iconImage.enabled = false;
        if (_rarityBorder != null) _rarityBorder.color = _emptyBorderColor;
        if (_tooltip != null) _tooltip.Hide(); // The tooltip was showing the item that just left.

        if (hadItem) OnCleared?.Invoke(this);
    }

    /// <summary>Selects or deselects the slot and shows or hides the highlight.</summary>
    public void SetSelected(bool selected)
    {
        Selected = selected;
        if (_selectedHighlight != null) _selectedHighlight.SetActive(selected);
    }

    /// <summary>Fires OnClicked. Called by the EventSystem.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(this);
    }

    /// <summary>Shows the tooltip for this slot's item. Called by the EventSystem.</summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log(
            $"Hovered {name}, Item: {_item != null}, Tooltip: {_tooltip != null}"
        );
        if (_item != null && _tooltip != null)
        {
            _tooltip.Show(_item);
        }
            
    }

    /// <summary>Hides the tooltip. Called by the EventSystem.</summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (_tooltip != null) _tooltip.Hide();
    }
}

/// <summary>Which kind of screen a slot is on.</summary>
enum SlotType
{
    Inventory,
    Reward,
    Shop,
    Equipabble
}
