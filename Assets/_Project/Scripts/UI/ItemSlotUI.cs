using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One item slot in the UI. Shows an item's icon with a border in its rarity color, and highlights when selected.
/// Only takes items of its assigned category. ItemGridUI and ConsumableSlotsUI fill these slots.
/// </summary>
public class ItemSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Only items of this category can go in this slot.")]
    [SerializeField] private ItemCategory _assignedCategory;

    [SerializeField] private Image _iconImage;
    [Tooltip("Turned on while the slot is selected.")]
    [SerializeField] private GameObject _selectedHighlight;
    [Tooltip("Colored from GlobalSettings.RarityColorSettings.")]
    [SerializeField] private Image _rarityBorder;

    private IInventoryItem _item;
    private Color _emptyBorderColor; // The border color set in the prefab, used when empty.

    /// <summary>True while the slot is selected. Set by ItemGridUI.</summary>
    [ShowInInspector, ReadOnly] public bool Selected { get; private set; }

    /// <summary>The item in this slot, or null.</summary>
    public IInventoryItem Item => _item;
    /// <summary>True when the slot has no item.</summary>
    public bool IsEmpty => _item == null;

    /// <summary>Fires when the slot is clicked, with this slot. ItemGridUI selects it.</summary>
    public event Action<ItemSlotUI> OnClicked;

    /// <summary>Fires when a slot that had an item is emptied, with this slot.</summary>
    public event Action<ItemSlotUI> OnCleared;

    private void Awake()
    {
        if (_rarityBorder != null) _emptyBorderColor = _rarityBorder.color;
        if (_selectedHighlight != null) _selectedHighlight.SetActive(false);
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
            Clear();
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

    /// <summary>Empties the slot, resets the border, and fires OnCleared if there was an item.</summary>
    public void Clear()
    {
        bool hadItem = _item != null;

        _item = null;
        _iconImage.sprite = null;
        _iconImage.enabled = false;
        if (_rarityBorder != null) _rarityBorder.color = _emptyBorderColor;

        if (hadItem) OnCleared?.Invoke(this);
    }

    /// <summary>Selects or deselects the slot and shows or hides the highlight.</summary>
    public void SetSelected(bool selected)
    {
        Selected = selected;
        if (_selectedHighlight != null) _selectedHighlight.SetActive(selected);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(this);
    }
}

