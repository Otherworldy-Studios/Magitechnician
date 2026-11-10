using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// What the player carries: crystals, owned and equipped charms, and consumables. On the player prefab.
/// Equipping a charm or using a consumable sends each of its modifiers to the player's Stats if that StatBlock has the
/// stat, otherwise to the gun's. The 1, 2, and 3 keys use the consumable in that slot.
/// Crystals and charms carry between scenes through PersistentProgress; consumables reset every run.
/// </summary>
public class Inventory : SerializedMonoBehaviour
{
    [Tooltip("Crystals the player has. PersistentProgress carries them between scenes.")]
    [SerializeField, FormerlySerializedAs("Crystals")] private int _crystals;
    [Tooltip("Charm slots in use.")]
    [SerializeField, FormerlySerializedAs("EquippedCharmSlots")] private int _equippedCharmSlots;
    [Tooltip("How many charms the player can own. 0 refuses every charm.")]
    [SerializeField, FormerlySerializedAs("TotalCharmSlots")] private int _charmSlots;
    [Tooltip("How many consumables the player can carry, one per key. 0 refuses every consumable.")]
    [SerializeField, FormerlySerializedAs("MaxConsumbables")] private int _maxConsumables;

    public int Crystals => _crystals;
    public int EquippedCharmSlots => _equippedCharmSlots;
    /// <summary>How many charms the player can own.</summary>
    public int CharmSlots => _charmSlots;
    /// <summary>How many consumables the player can carry.</summary>
    public int MaxConsumables => _maxConsumables;

    private List<Charm> _ownedCharms = new();
    private List<Charm> _equippedCharms = new();
    private List<Consumable> _consumables = new(); // In key order: index 0 is the 1 key.

    // Shown read-only in the Inspector through Odin, so the lists can be checked in Play mode.
    [ShowInInspector, ReadOnly]
    public IReadOnlyList<Charm> OwnedCharms => _ownedCharms;

    [ShowInInspector, ReadOnly]
    public IReadOnlyList<Charm> EquippedCharms => _equippedCharms;

    [ShowInInspector, ReadOnly]
    public IReadOnlyList<Consumable> Consumables => _consumables;

    /// <summary>Fires when crystals change, with the new total. CurrencyDisplay listens to it.</summary>
    public event Action<int> OnCrystalsChanged;
    /// <summary>Fires when charms or consumables are added, removed, equipped, or used. ConsumableSlotsUI listens to it.</summary>
    public event Action OnInventoryChanged;

    // INTEGRATION: new. PlayerRoot.AssignStats hands these over in Awake, so charm and consumable modifiers can reach
    // either Stats. ApplyModifier picks between them.
    [Tooltip("The player's Stats. PlayerRoot fills this in Awake if it's empty.")]
    [SerializeField] private Stats _playerStats;
    [Tooltip("The gun's Stats. PlayerRoot fills this in Awake.")]
    [SerializeField] private Stats _gunStats;
    [Tooltip("Healed by consumables. Found through PlayerRoot if empty.")]
    [SerializeField] private Health _health; // INTEGRATION: ResolveHealth fills it, since nothing assigned it before.
    [Tooltip("Only use consumables during a run. Off allows them in the Hub too.")]
    [SerializeField] private bool _onlyDuringRun = true;

    private bool _subscribed; // INTEGRATION: new. True once listening to InputManager, so it never subscribes twice.

    /// <summary>Gives the Inventory the player's Stats. Keeps a Stats already assigned in the Inspector.</summary>
    // INTEGRATION: new. Called by PlayerRoot.AssignStats.
    public void SetStats(Stats playerStats)
    {
        if (_playerStats == null)
        {
            _playerStats = playerStats;
        }
    }

    /// <summary>Gives the Inventory the gun's Stats, for charm and consumable modifiers on gun stats such as FireRate.</summary>
    // INTEGRATION: new. Called by PlayerRoot.AssignStats with Gun.Stats.
    public void SetGunStats(Stats gunStats)
    {
        _gunStats = gunStats;
    }

    /// <summary>Listens to the consumable keys.</summary>
    private void OnEnable()
    {
        SubscribeToInput();
    }

    /// <summary>Listens to the consumable keys, in case InputManager didn't exist yet in OnEnable.</summary>
    private void Start()
    {
        SubscribeToInput();
    }

    /// <summary>Stops listening to the consumable keys.</summary>
    private void OnDisable()
    {
        if (_subscribed && InputManager.HasInstance)
            InputManager.Instance.OnConsumablePressed -= HandleConsumablePressed;

        _subscribed = false;
    }

    /// <summary>Subscribes to InputManager.OnConsumablePressed once.</summary>
    // INTEGRATION: new. Connects the 1, 2, and 3 keys to UseConsumable.
    private void SubscribeToInput()
    {
        if (_subscribed || !InputManager.HasInstance) return;

        InputManager.Instance.OnConsumablePressed += HandleConsumablePressed;
        _subscribed = true;
    }

    /// <summary>
    /// Uses the consumable in the pressed slot. The keys only fire in Gameplay input mode, which includes the Hub,
    /// so this also checks for the Playing state while Only During Run is on.
    /// </summary>
    /// <param name="slot">0, 1, or 2.</param>
    private void HandleConsumablePressed(int slot)
    {
        if (_onlyDuringRun && GameManager.HasInstance &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        UseConsumable(slot);
    }

    /// <summary>
    /// Adds a charm or consumable. Returns false if the item is null, isn't a charm or consumable, or its list is full
    /// (CharmSlots for charms, MaxConsumables for consumables). Treasure chests and the shop call this.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public bool TryAddItem(IInventoryItem item)
    {
        switch (item)
        {
            case Charm charm:
                if (_ownedCharms.Count >= _charmSlots)
                {
                    return false;
                }
                _ownedCharms.Add(charm);
                break;

            case Consumable consumable:
                if (_consumables.Count >= _maxConsumables)
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

    /// <summary>Returns every owned charm and carried consumable.</summary>
    public IEnumerable<IInventoryItem> GetAllItems()
    {
        return _ownedCharms
            .Cast<IInventoryItem>()
            .Concat(_consumables.Cast<IInventoryItem>());
    }

    /// <summary>Removes every charm, and first removes the equipped ones' modifiers.</summary>
    // INTEGRATION: new. PersistentProgress.LoadInto calls it before adding the saved charms back.
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

    /// <summary>Removes every consumable.</summary>
    public void ClearConsumables()
    {
        _consumables.Clear();
        OnInventoryChanged?.Invoke();
    }

    /// <summary>Sets the crystal total directly and fires OnCrystalsChanged.</summary>
    /// <param name="amount">The new total.</param>
    // Crystals is read-only, so PersistentProgress.LoadInto sets the saved total through this.
    public void SetCrystals(int amount)
    {
        _crystals = amount;
        OnCrystalsChanged?.Invoke(_crystals);
    }

    /// <summary>Adds crystals and fires OnCrystalsChanged. Treasure chests call this.</summary>
    public void AddCrystals(int amount)
    {
        _crystals += amount;
        OnCrystalsChanged?.Invoke(_crystals);
    }

    /// <summary>Removes crystals and fires OnCrystalsChanged. Known bug, fixed in task 11: it can go below 0.</summary>
    public void SpendCrystals(int amount)
    {
        _crystals -= amount;
        OnCrystalsChanged?.Invoke(_crystals);
    }

    /// <summary>
    /// Equips an owned charm and applies its modifiers, with the charm as the source.
    /// Does nothing if the charm isn't owned or is already equipped. Doesn't check charm slots yet (task 12).
    /// </summary>
    public void EquipCharm(Charm charm)
    {
        if (!_ownedCharms.Contains(charm)) return;
        if (_equippedCharms.Contains(charm)) return; // INTEGRATION: new. A charm can't be equipped twice.

        _equippedCharms.Add(charm);
        foreach (StatModifier mod in charm.Modifiers)
        {
            ApplyModifier(mod, charm); // INTEGRATION: goes to the player's or the gun's Stats, with the charm as source.
        }

        OnInventoryChanged?.Invoke();
    }

    /// <summary>Unequips a charm and removes everything it added, from both Stats.</summary>
    public void UnequipCharm(Charm charm)
    {
        if (!_equippedCharms.Contains(charm)) return;

        RemoveModifiers(charm); // INTEGRATION: removes by source instead of each modifier asset.

        _equippedCharms.Remove(charm);
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Uses the consumable at a slot: applies its modifiers, heals, removes the modifiers again when its duration ends,
    /// and takes it out of the list. ConsumableSlotsUI updates from OnInventoryChanged.
    /// </summary>
    /// <param name="index">The slot, 0 to 2.</param>
    // INTEGRATION: checks the index, removes the used consumable, heals through ResolveHealth, and times the modifiers.
    public void UseConsumable(int index)
    {
        if (index < 0 || index >= _consumables.Count) return;

        Consumable consumable = _consumables[index];

        // A timed use gets its own source object, so using the same potion twice doesn't end the first one early
        // when RemoveAfter removes by source. An untimed use keeps the consumable as its source.
        object source = consumable.Duration > 0f ? new object() : consumable;

        foreach (StatModifier mod in consumable.Modifiers)
        {
            ApplyModifier(mod, source);
        }

        Health health = ResolveHealth();
        if (consumable.InstantHeal > 0 && health != null)
        {
            health.RegenHealth(consumable.InstantHeal);
        }

        // Mark consumable modifiers Permanent, so Stats doesn't remove them before this does.
        if (consumable.Duration > 0f && consumable.Modifiers.Count > 0)
            StartCoroutine(RemoveAfter(source, consumable.Duration));

        _consumables.RemoveAt(index);
        OnInventoryChanged?.Invoke();
    }

    /// <summary>Waits in game time, so pausing pauses the timer, then removes one use's modifiers.</summary>
    private IEnumerator RemoveAfter(object source, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        RemoveModifiers(source);
    }

    /// <summary>Returns the assigned Health, or finds the player's through PlayerRoot.</summary>
    private Health ResolveHealth()
    {
        if (_health == null)
        {
            PlayerRoot root = GetComponentInParent<PlayerRoot>();
            _health = root != null ? root.PlayerHealth : GetComponentInParent<Health>();
        }

        return _health;
    }

    /// <summary>
    /// Sends a modifier to the player's Stats if its StatBlock has the stat, otherwise to the gun's, and warns if
    /// neither has it. The same routing PlayerRoot.ApplyModifiers uses for gun parts.
    /// </summary>
    // INTEGRATION: new. Replaces adding every modifier to the player's Stats, which never changed gun stats.
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

    /// <summary>Removes everything from one source, from both Stats.</summary>
    private void RemoveModifiers(object source)
    {
        if (_playerStats != null) _playerStats.RemoveAllFromSource(source);
        if (_gunStats != null) _gunStats.RemoveAllFromSource(source);
    }

    /// <summary>
    /// Debug: adds any item asset dragged into the field, and logs whether it was added with the current counts.
    /// </summary>
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
                  $"Consumables {Consumables.Count}/{_maxConsumables}, charms {OwnedCharms.Count}/{_charmSlots}.", this);
    }
}
