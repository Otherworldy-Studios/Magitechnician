using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A group of ItemSlotUI slots where one can be selected at a time. Shares one tooltip between its slots. Screens
/// listen to OnSelectionChanged to act on the chosen item.
/// </summary>
// INTEGRATION: new. Uses ItemSlotUI.OnClicked and OnCleared (both new) and ItemSlotUI.SetTooltip.
public class ItemGridUI : MonoBehaviour
{
    [Tooltip("One tooltip shared by every slot in this grid.")]
    [SerializeField] private ItemTooltip _tooltip;

    // Every slot in the grid. Filled in Awake and by Register. Read-only in the Inspector.
    [SerializeField, Sirenix.OdinInspector.ReadOnly] private List<ItemSlotUI> _slots = new();
    private ItemSlotUI _selected;

    /// <summary>The selected slot, or null.</summary>
    public ItemSlotUI Selected => _selected;

    /// <summary>Fires with the new selection, or null when nothing is selected.</summary>
    public event Action<ItemSlotUI> OnSelectionChanged;

    /// <summary>Hides the tooltip and registers every slot in children.</summary>
    private void Awake()
    {
        if (_tooltip != null) _tooltip.Hide();

        foreach (ItemSlotUI slot in GetComponentsInChildren<ItemSlotUI>(true).ToList())
        {
            Register(slot);
        }
    }

    /// <summary>
    /// Adds a slot to the grid: gives it the tooltip and listens to its clicks. Call this for slots created at run time,
    /// for example on the reward screen. A slot already in the grid is ignored.
    /// </summary>
    public void Register(ItemSlotUI slot)
    {
        if (slot == null || _slots.Contains(slot)) return;

        _slots.Add(slot);
        slot.SetTooltip(_tooltip);
        slot.OnClicked += SelectSlot;
        slot.OnCleared += HandleSlotCleared;
    }

    /// <summary>Stops listening to every slot.</summary>
    private void OnDestroy()
    {
        foreach (ItemSlotUI slot in _slots)
        {
            if (slot == null) continue;
            slot.OnClicked -= SelectSlot;
            slot.OnCleared -= HandleSlotCleared;
        }
    }

    /// <summary>Selects a slot and deselects the previous one. Null clears the selection. Runs on each slot's OnClicked.</summary>
    public void SelectSlot(ItemSlotUI slot)
    {
        if (_selected == slot) return;

        if (_selected != null) _selected.SetSelected(false);
        _selected = slot;
        if (_selected != null) _selected.SetSelected(true);

        OnSelectionChanged?.Invoke(_selected);
    }

    /// <summary>Deselects a slot whose item was used, sold, or removed. Runs on each slot's OnCleared.</summary>
    private void HandleSlotCleared(ItemSlotUI slot)
    {
        if (slot == _selected) SelectSlot(null);
    }
}