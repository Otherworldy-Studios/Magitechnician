using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// A group of ItemSlotUI slots where one can be selected at a time. Shows the selected item in its info box.
/// Screens listen to OnSelectionChanged to act on the chosen item.
/// </summary>
public class ItemGridUI : MonoBehaviour
{
    [Tooltip("Shows the selected item. Several grids can share one.")]
    [SerializeField] private ItemTooltip _tooltip;

    [Tooltip("Every slot in the grid. Filled in Awake and by Register.")]
    [SerializeField, ReadOnly] private List<ItemSlotUI> _slots = new();

    private ItemSlotUI _selected;

    /// <summary>The selected slot, or null.</summary>
    public ItemSlotUI Selected => _selected;

    /// <summary>Fires with the new selection, or null when nothing is selected.</summary>
    public event Action<ItemSlotUI> OnSelectionChanged;

    private void Awake()
    {
        if (_tooltip != null) _tooltip.Clear();

        foreach (ItemSlotUI slot in GetComponentsInChildren<ItemSlotUI>(true))
        {
            Register(slot);
        }
    }

    private void OnDestroy()
    {
        foreach (ItemSlotUI slot in _slots)
        {
            if (slot == null) continue;
            slot.OnClicked -= HandleSlotClicked;
            slot.OnCleared -= HandleSlotCleared;
        }
    }

    /// <summary>
    /// Adds a slot to the grid and listens to its clicks. Call this for slots created at run
    /// time, for example on the reward screen. A slot already in the grid is ignored.
    /// </summary>
    public void Register(ItemSlotUI slot)
    {
        if (slot == null || _slots.Contains(slot)) return;

        _slots.Add(slot);
        slot.OnClicked += HandleSlotClicked;
        slot.OnCleared += HandleSlotCleared;
    }

    /// <summary>Selects a slot and deselects the previous one. Null clears the selection.</summary>
    public void SelectSlot(ItemSlotUI slot)
    {
        if (_selected == slot) return;

        if (_selected != null) _selected.SetSelected(false);
        _selected = slot;
        if (_selected != null) _selected.SetSelected(true);

        if (_tooltip != null) _tooltip.Show(_selected != null ? _selected.Item : null, this);

        OnSelectionChanged?.Invoke(_selected);
    }

    private void HandleSlotClicked(ItemSlotUI slot)
    {
        SelectSlot(slot == _selected ? null : slot);
    }

    /// <summary>Deselects a slot whose item was used, sold, or removed. Runs on each slot's OnCleared.</summary>
    private void HandleSlotCleared(ItemSlotUI slot)
    {
        if (slot == _selected) SelectSlot(null);
    }
}