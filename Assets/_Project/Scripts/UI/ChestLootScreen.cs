using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChestLootScreen : UIScreen
{
    [Header("References")]
    [SerializeField] private Button _continueButton;
    [SerializeField] private TMP_Text _crystalsText;
    [Tooltip("Shows the consumable's name, description, and icon. Hidden when the chest gave no consumable.")]
    [SerializeField] private ItemTooltip _itemTooltip;
    [Tooltip("Shown when the chest had a consumable but the consumable slots were full. Optional.")]
    [SerializeField] private GameObject _slotsFullText;

    [Header("Full Slots")]
    [Tooltip("Holds the held consumables and the Replace button. Shown only when the slots were full.")]
    [SerializeField] private GameObject _replacePanel;
    [Tooltip("The grid around the held slots. Leave its Tooltip empty, or give it its own, so selecting a held " +
             "consumable doesn't replace the chest's consumable in Item Tooltip.")]
    [SerializeField] private ItemGridUI _heldGrid;
    [Tooltip("One per consumable slot, in key order. Each slot's Assigned Category must be Consumable.")]
    [SerializeField] private ItemSlotUI[] _heldSlots;
    [Tooltip("Discards the selected held consumable and takes the chest's consumable in its slot.")]
    [SerializeField] private Button _replaceButton;

    private Inventory _inventory;
    private Consumable _offered; // The chest's consumable that didn't fit. Null unless the player can replace one.

    private void Awake()
    {
        _continueButton.onClick.AddListener(Close);

        if (_replaceButton != null) _replaceButton.onClick.AddListener(ReplaceSelected);
        if (_heldGrid != null) _heldGrid.OnSelectionChanged += HandleSelectionChanged;
    }

    private void OnDestroy()
    {
        _continueButton.onClick.RemoveListener(Close);

        if (_replaceButton != null) _replaceButton.onClick.RemoveListener(ReplaceSelected);
        if (_heldGrid != null) _heldGrid.OnSelectionChanged -= HandleSelectionChanged;
    }

    public void ShowLoot(int crystals, Consumable consumable, bool consumableAdded)
    {
        _crystalsText.gameObject.SetActive(crystals > 0);
        _crystalsText.text = $"+{crystals} Crystals";

        bool hasConsumable = consumable != null;
        _itemTooltip.gameObject.SetActive(hasConsumable);

        if (hasConsumable)
            _itemTooltip.Show(consumable, this);
        else
            _itemTooltip.Clear();

        bool slotsFull = hasConsumable && !consumableAdded;

        if (_slotsFullText != null)
            _slotsFullText.SetActive(slotsFull);

        ShowHeldConsumables(slotsFull ? consumable : null);
    }

 
    private void ShowHeldConsumables(Consumable offered)
    {
        _inventory = offered != null ? FindPlayerInventory() : null;
        _offered = _inventory != null ? offered : null;

        bool canReplace = _offered != null && _replacePanel != null && _heldGrid != null && _replaceButton != null;
        if (_replacePanel != null) _replacePanel.SetActive(canReplace);
        if (!canReplace) return;

        _heldGrid.SelectSlot(null);
        _replaceButton.interactable = false;

        for (int i = 0; i < _heldSlots.Length; i++)
        {
            bool hasItem = i < _inventory.Consumables.Count;
            _heldSlots[i].gameObject.SetActive(hasItem);

            if (hasItem)
                _heldSlots[i].SetItem(_inventory.Consumables[i]);
            else
                _heldSlots[i].Clear();
        }
    }

    private void HandleSelectionChanged(ItemSlotUI selectedSlot)
    {
        if (_replaceButton != null)
            _replaceButton.interactable = selectedSlot != null && !selectedSlot.IsEmpty;
    }

    private void ReplaceSelected()
    {
        if (_offered == null || _inventory == null || _heldGrid.Selected == null) return;

        int index = Array.IndexOf(_heldSlots, _heldGrid.Selected);
        if (_inventory.ReplaceConsumable(index, _offered))
            Close();
    }

   
    private void Close()
    {
        foreach (ItemSlotUI slot in _heldSlots)
        {
            if (slot != null) slot.Clear();
        }

        _inventory = null;
        _offered = null;

        UIManager.Instance.ResumeButtonPressed();
    }

    private static Inventory FindPlayerInventory()
    {
        if (!GameManager.HasInstance || GameManager.Instance.Player == null) return null;

        PlayerRoot root = GameManager.Instance.Player.GetComponent<PlayerRoot>();
        return root != null ? root.Inventory : null;
    }
}