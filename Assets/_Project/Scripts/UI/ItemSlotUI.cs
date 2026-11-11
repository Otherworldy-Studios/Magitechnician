using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
// CHANGED: removed the unused "using Unity.VisualScripting;"

public class ItemSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler // CHANGED: IPointerClickHandler
{
    [SerializeField] private ItemCategory _assignedCategory;
    [SerializeField] private Image _iconImage;
    [SerializeField] private GameObject _selectedHighlight;
    [SerializeField] private Image rarityBorder;
    [SerializeField] private ItemTooltip _tooltip;
    [SerializeField] private SlotType _slotType = SlotType.Inventory;

    [ShowInInspector, ReadOnly] public bool Selected { get; private set; }

    private IInventoryItem _item;
    private Color _emptyBorderColor; // CHANGED: new

    public IInventoryItem Item => _item;
    public bool IsEmpty => _item == null;

    public event Action<ItemSlotUI> OnClicked;
    public event Action<ItemSlotUI> OnCleared; // CHANGED: new. Lets the grid deselect a slot that was emptied.

    private void Awake()
    {
        if (rarityBorder != null) _emptyBorderColor = rarityBorder.color; // CHANGED: the color set in the prefab
        if (_selectedHighlight != null) _selectedHighlight.SetActive(false); // CHANGED: start unselected
    }

   
    public void SetTooltip(ItemTooltip tooltip)
    {
        if (_tooltip == null) _tooltip = tooltip;
    }

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
            Clear(); // CHANGED: was only a return, which left the old item showing
            return;
        }

        _item = item;
        _iconImage.sprite = item.Icon;
        _iconImage.enabled = item.Icon != null;
        SetBorderColor(item.Rarity); // CHANGED: moved to a method with null checks
    }

    // CHANGED: new. Skips the color if the border or GlobalSettings is missing, instead of throwing.
    private void SetBorderColor(ItemRarity rarity)
    {
        if (rarityBorder == null) return;

        if (GlobalSettings.Instance == null)
        {
            Debug.LogWarning($"{name}: no GlobalSettings found in Resources, so the rarity border isn't colored.", this);
            return;
        }

        var colors = GlobalSettings.Instance.RarityColorSettings;
        rarityBorder.color = rarity switch
        {
            ItemRarity.Common => colors.Common,
            ItemRarity.Uncommon => colors.Uncommon,
            ItemRarity.Rare => colors.Rare,
            ItemRarity.Epic => colors.Epic,
            _ => _emptyBorderColor
        };
    }

    public void Clear()
    {
        bool hadItem = _item != null; // CHANGED

        _item = null;
        _iconImage.sprite = null;
        _iconImage.enabled = false;
        if (rarityBorder != null) rarityBorder.color = _emptyBorderColor; // CHANGED: reset the border
        if (_tooltip != null) _tooltip.Hide();                            // CHANGED: close the tooltip for this item

        if (hadItem) OnCleared?.Invoke(this); // CHANGED
    }

    public void SetSelected(bool selected)
    {
        Selected = selected;
        if (_selectedHighlight != null) _selectedHighlight.SetActive(selected);
    }

    public void OnPointerClick(PointerEventData eventData) // CHANGED: was OnPointerDown
    {
        OnClicked?.Invoke(this);
    }

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

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_tooltip != null) _tooltip.Hide();
    }
}

enum SlotType
{
    Inventory,
    Reward,
    Shop,
    Equipabble
}