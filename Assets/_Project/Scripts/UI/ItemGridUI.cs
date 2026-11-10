using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemGridUI : MonoBehaviour
{
    [SerializeField, Sirenix.OdinInspector.ReadOnly] private List<ItemSlotUI> _slots = new();
    private ItemSlotUI _selected;

    public ItemSlotUI Selected => _selected;
    public event Action<ItemSlotUI> OnSelectionChanged;

    private void Awake()
    {
        _slots = GetComponentsInChildren<ItemSlotUI>(true).ToList();
        foreach (ItemSlotUI slot in _slots)
        {
            slot.OnClicked += SelectSlot;
        }
    }

    private void OnDestroy()
    {
        foreach (ItemSlotUI slot in _slots)
        {
            if (slot != null) slot.OnClicked -= SelectSlot;
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
}