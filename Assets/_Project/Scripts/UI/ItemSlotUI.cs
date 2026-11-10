using System;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private ItemCategory _assignedCategory;
    [SerializeField] private Image _iconImage;
    [SerializeField] private GameObject _selectedHighlight;
    [SerializeField] private Image rarityBorder;
    [SerializeField] private ItemTooltip _tooltip;
    [SerializeField] private SlotType _slotType = SlotType.Inventory;

    [ShowInInspector, ReadOnly] public bool Selected { get; private set; }

    private IInventoryItem _item;

    public IInventoryItem Item => _item;
    public bool IsEmpty => _item == null;

    public event Action<ItemSlotUI> OnClicked;

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
            return;
        }

        _item = item;
        _iconImage.sprite = item.Icon;
        _iconImage.enabled = item.Icon != null;
        switch(item.Rarity)
        {
            case ItemRarity.Common:
                rarityBorder.color = GlobalSettings.Instance.RarityColorSettings.Common;
                break;
            case ItemRarity.Uncommon:
                rarityBorder.color = GlobalSettings.Instance.RarityColorSettings.Uncommon;
                break;
            case ItemRarity.Rare:
                rarityBorder.color = GlobalSettings.Instance.RarityColorSettings.Rare;
                break;
            case ItemRarity.Epic:
                rarityBorder.color = GlobalSettings.Instance.RarityColorSettings.Epic;
                break;


        }
    }

    public void Clear()
    {
        _item = null;
        _iconImage.sprite = null;
        _iconImage.enabled = false;
    }

    public void SetSelected(bool selected)
    {
        Selected = selected;
        if (_selectedHighlight != null) _selectedHighlight.SetActive(selected);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnClicked?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_item != null && _tooltip != null) _tooltip.Show(_item);
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