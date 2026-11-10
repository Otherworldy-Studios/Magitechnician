using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemGridUI : MonoBehaviour
{
    [Tooltip("One tooltip shared by every slot in this grid.")] // CHANGED: new
    [SerializeField] private ItemTooltip _tooltip;               // CHANGED: new

    [SerializeField, Sirenix.OdinInspector.ReadOnly] private List<ItemSlotUI> _slots = new();
    private ItemSlotUI _selected;

    public ItemSlotUI Selected => _selected;
    public event Action<ItemSlotUI> OnSelectionChanged;

    private void Awake()
    {
        if (_tooltip != null) _tooltip.Hide(); // CHANGED: start hidden

        foreach (ItemSlotUI slot in GetComponentsInChildren<ItemSlotUI>(true).ToList()) // CHANGED: goes through Register
        {
            Register(slot);
        }
    }

    // CHANGED: new. Call this for slots or item cards created at run time, for example on the reward screen.
    public void Register(ItemSlotUI slot)
    {
        if (slot == null || _slots.Contains(slot)) return;

        _slots.Add(slot);
        slot.SetTooltip(_tooltip);
        slot.OnClicked += SelectSlot;
        slot.OnCleared += HandleSlotCleared;
    }

    private void OnDestroy()
    {
        foreach (ItemSlotUI slot in _slots)
        {
            if (slot == null) continue;
            slot.OnClicked -= SelectSlot;
            slot.OnCleared -= HandleSlotCleared; // CHANGED
        }
    }

    public void SelectSlot(ItemSlotUI slot)
    {
        if (_selected == slot) return;

        if (_selected != null) _selected.SetSelected(false);
        _selected = slot;
        if (_selected != null) _selected.SetSelected(true);

        OnSelectionChanged?.Invoke(_selected);
    }

    // CHANGED: new. Deselects a slot whose item was used, sold or removed.
    private void HandleSlotCleared(ItemSlotUI slot)
    {
        if (slot == _selected) SelectSlot(null);
    }
}