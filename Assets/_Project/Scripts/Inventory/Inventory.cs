using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

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



    [SerializeField] private Stats _playerStats;

    
    [SerializeField] private Stats _gunStats;  
    [SerializeField] private Health _health;
    [Tooltip("Only use consumables during a run. Off allows them in the Hub too.")]
    [SerializeField] private bool _onlyDuringRun = true;                     

    private bool _subscribed; 
    public void SetStats(Stats playerStats)
    {
        if (_playerStats == null)
        {
            _playerStats = playerStats;
        }
    }

  
    private void OnEnable()
    {
        SubscribeToInput();
    }

    private void Start()
    {
        SubscribeToInput();
    }

    private void OnDisable()
    {
        if (_subscribed && InputManager.HasInstance)
            InputManager.Instance.ConsumablePressed -= HandleConsumablePressed;

        _subscribed = false;
    }

    
    private void SubscribeToInput()
    {
        if (_subscribed || !InputManager.HasInstance) return;

        InputManager.Instance.ConsumablePressed += HandleConsumablePressed;
        _subscribed = true;
    }

   
    private void HandleConsumablePressed(int slot)
    {
        if (_onlyDuringRun && GameManager.HasInstance &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        UseConsumable(slot);
    }

    public void SetGunStats(Stats gunStats)
    {
        _gunStats = gunStats;
    }

    public bool TryAddItem(IInventoryItem item)
    {
        switch (item)
        {
            case Charm charm:
                if (_ownedCharms.Count >= TotalCharmSlots)
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
            RemoveModifiers(charm);
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
        if (_equippedCharms.Contains(charm)) return; 

        _equippedCharms.Add(charm);
        foreach (StatModifier mod in charm.statModifiers)
        {
            ApplyModifier(mod, charm);
        }

        OnInventoryChanged?.Invoke();
    }

    public void UnequipCharm(Charm charm)
    {
        if (!_equippedCharms.Contains(charm)) return;

        RemoveModifiers(charm); 

        _equippedCharms.Remove(charm);
        OnInventoryChanged?.Invoke();
    }


    public void UseConsumable(int index)
    {
        if (index < 0 || index >= _consumables.Count) return;

        Consumable consumable = _consumables[index];

        
        object source = consumable.duration > 0f ? new object() : consumable;

        foreach (StatModifier mod in consumable.statModifiers)
        {
            ApplyModifier(mod, source); 
        }

        Health health = ResolveHealth(); 
        if (consumable.instantHeal > 0 && health != null)
        {
            health.RegenHealth(consumable.instantHeal);
        }

        
        if (consumable.duration > 0f && consumable.statModifiers.Count > 0)
            StartCoroutine(RemoveAfter(source, consumable.duration));

        _consumables.RemoveAt(index);
        OnInventoryChanged?.Invoke();
    }

   
    private IEnumerator RemoveAfter(object source, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        RemoveModifiers(source);
    }

   
    private Health ResolveHealth()
    {
        if (_health == null)
        {
            PlayerRoot root = GetComponentInParent<PlayerRoot>();
            _health = root != null ? root.PlayerHealth : GetComponentInParent<Health>();
        }

        return _health;
    }


    private void ApplyModifier(StatModifier mod, object source)
    {
        if (mod == null || mod.Stat == null) return;

        if (_playerStats != null && _playerStats.HasStat(mod.Stat))
            _playerStats.AddModifier(mod, source);
        else if (_gunStats != null && _gunStats.HasStat(mod.Stat))
            _gunStats.AddModifier(mod, source);
        else
            Debug.LogWarning($"{mod.Stat.name} isn't on the player's or the gun's StatBlock. Modifier from {source} skipped.");
    }

    private void RemoveModifiers(object source)
    {
        if (_playerStats != null) _playerStats.RemoveAllFromSource(source);
        if (_gunStats != null) _gunStats.RemoveAllFromSource(source);
    }

    [Button, DisableInEditorMode]
    private void DEBUGAddItem(ScriptableObject item)
    {
        if (item is not IInventoryItem inventoryItem)
        {
            Debug.LogWarning($"{(item != null ? item.name : "null")} isn't an IInventoryItem.", this);
            return;
        }

        bool added = TryAddItem(inventoryItem);
        Debug.Log($"Add {inventoryItem.DisplayName} ({inventoryItem.Category}): {(added ? "added" : "refused")}. " +
                  $"Consumables {Consumables.Count}/{MaxConsumbables}, charms {OwnedCharms.Count}/{TotalCharmSlots}.", this);
    }
}