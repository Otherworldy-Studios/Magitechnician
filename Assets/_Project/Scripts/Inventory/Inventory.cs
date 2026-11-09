using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class Inventory : SerializedMonoBehaviour
{
    public int Crystals;
    public int EquippedCharmSlots;
    public int TotalCharmSlots;
    public int MaxConsumbables;

    private List<Charm> _ownedCharms = new();
    private List<Charm> _equippedCharms = new();
    private List<Consumable> _consumables = new();

    [ShowInInspector, ReadOnly]
    public IReadOnlyList<Charm> OwnedCharms => _ownedCharms;

    [ShowInInspector, ReadOnly]
    public IReadOnlyList<Charm> EquippedCharms => _equippedCharms;

    [ShowInInspector, ReadOnly]
    public IReadOnlyList<Consumable> Consumables => _consumables;

    public event Action<int> OnCrystalsChanged;
    public event Action OnInventoryChanged;

    [SerializeField] private Stats stats;

    public bool TryAddItem(IInventoryItem item)
    {
        switch (item)
        {
            case Charm charm:
                if(_ownedCharms.Count >= TotalCharmSlots)
                {
                    return false;
                }
                _ownedCharms.Add(charm);
                break;

            case Consumable consumable:
                if (_consumables.Count >= MaxConsumbables)
                {
                    return false;
                }
                _consumables.Add(consumable);
                break;

            case null:
                return false;

            default:
                return false;
        }
        OnInventoryChanged?.Invoke();
        return true;
    }

    public IEnumerable<IInventoryItem> GetAllItems()
    {
        return _ownedCharms
            .Cast<IInventoryItem>()
            .Concat(_consumables.Cast<IInventoryItem>());
    }

    public void ClearCharms()
    {
        foreach (Charm charm in _equippedCharms)
        {
            stats.RemoveAllFromSource(charm);
        }
        _equippedCharms.Clear();
        _ownedCharms.Clear();
        OnInventoryChanged?.Invoke();
    }

    public void ClearConsumables()
    {
        _consumables.Clear();
        OnInventoryChanged?.Invoke();
    }

    public void AddCrystals(int amount)
    {
        Crystals += amount;
        OnCrystalsChanged?.Invoke(Crystals);
    }

    public void SpendCrystals(int amount)
    {
        Crystals -= amount;
        OnCrystalsChanged?.Invoke(Crystals);
    }

    public void EquipCharm(Charm charm)
    {
        if (!_ownedCharms.Contains(charm)) return;
        _equippedCharms.Add(charm);
        foreach(StatModifier mod in charm.statModifiers)
        {
            stats.AddModifier(mod);
        }

        OnInventoryChanged?.Invoke();
    }

    public void UnequipCharm(Charm charm)
    {
        if (!_equippedCharms.Contains(charm)) return;
        
      stats.RemoveAllFromSource(charm);
        
        _equippedCharms.Remove(charm);
        OnInventoryChanged?.Invoke();
    }

    public void UseConsumable(int index)
    {
        _consumables[index].Use(transform.parent.gameObject);
    }
}
