using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

/// <summary>
/// What the player carries: crystals, owned and equipped charms, and consumables. On the player prefab.
/// Equipping a charm or using a consumable sends each of its modifiers to the player's Stats if that StatBlock has the
/// stat, otherwise to the gun's. The 1, 2, and 3 keys use the consumable in that slot.
/// Crystals and charms carry between scenes through PersistentProgress; consumables reset every run.
/// </summary>
public class Inventory : SerializedMonoBehaviour
{
    [Tooltip("Crystals the player has. PersistentProgress carries them between scenes.")]
    [SerializeField] private int _crystals;
    [Tooltip("How many charms the player can equip at once. 0 prevents equipping any charm.")]
    [SerializeField] private int _equippedCharmSlots;
    [Tooltip("How many consumables the player can carry, one per key. 0 refuses every consumable.")]
    [SerializeField] private int _maxConsumables;

    [OdinSerialize, ReadOnly] private int _totalCharms => ItemDatabase.Instance.Charms.Count;

    public int Crystals => _crystals;
    /// <summary>How many charms the player can equip at once.</summary>
    public int EquippedCharmSlots => _equippedCharmSlots;
    public int TotalCharms => _totalCharms;
    /// <summary>How many consumables the player can carry.</summary>
    public int MaxConsumables => _maxConsumables;

    private readonly List<Charm> _ownedCharms = new();
    private readonly List<Charm> _equippedCharms = new();
    private readonly List<Consumable> _consumables = new(); // Index 0 is the 1 key.

    [ShowInInspector, ReadOnly]
    public IReadOnlyList<Charm> OwnedCharms => _ownedCharms;

    [ShowInInspector, ReadOnly]
    public IReadOnlyList<Charm> EquippedCharms => _equippedCharms;

    [ShowInInspector, ReadOnly]
    public IReadOnlyList<Consumable> Consumables => _consumables;

    /// <summary>Fires when crystals change, with the new total.</summary>
    public event Action<int> OnCrystalsChanged;
    /// <summary>Fires when charms or consumables are added, removed, replaced, equipped, or used.</summary>
    public event Action OnInventoryChanged;
    /// <summary>Fires when a consumable is used, with that consumable.</summary>
    public event Action<Consumable> OnConsumableUsed;
    /// <summary>Fires when a timed consumable's modifiers are removed at the end of its duration.</summary>
    public event Action<Consumable> OnConsumableEnded;

    [SerializeField] private Stats _playerStats;
    [SerializeField] private PlayerAbilities _playerAbilities;
    [SerializeField] private Stats _gunStats;
    [SerializeField] private Health _health;
    [Tooltip("Only use consumables during a run. Off allows them in the Hub too.")]
    [SerializeField] private bool _onlyDuringRun = true;
    [SerializeField] private PlayerRoot _playerRoot;
    [SerializeField] private ParticleSystem _consumableUseParticles;

    private bool _subscribed;

    private void Awake()
    {
        if (_playerRoot == null)
        {
            _playerRoot = GetComponentInParent<PlayerRoot>();
            if (_playerRoot == null)
                Debug.LogWarning($"{nameof(Inventory)} on {name} is missing a {nameof(PlayerRoot)} reference.", this);
        }
    }

    /// <summary>Gives the Inventory the player's Stats. Keeps a Stats already assigned in the Inspector.</summary>
    public void SetStats(Stats playerStats)
    {
        if (_playerStats == null)
            _playerStats = playerStats;
    }

    /// <summary>Gives the Inventory the player's abilities. Keeps one already assigned in the Inspector.</summary>
    public void SetAbilities(PlayerAbilities abilities)
    {
        if (_playerAbilities == null)
            _playerAbilities = abilities;
    }

    /// <summary>Gives the Inventory the gun's Stats, for charm and consumable modifiers on gun stats such as FireRate.</summary>
    public void SetGunStats(Stats gunStats)
    {
        _gunStats = gunStats;
    }

    private void OnEnable()
    {
        SubscribeToInput();
    }

    /// <summary>Subscribes again in case InputManager didn't exist yet in OnEnable.</summary>
    private void Start()
    {
        SubscribeToInput();
    }

    private void OnDisable()
    {
        if (_subscribed && InputManager.HasInstance)
            InputManager.Instance.OnConsumablePressed -= HandleConsumablePressed;

        _subscribed = false;
    }

    private void SubscribeToInput()
    {
        if (_subscribed || !InputManager.HasInstance) return;

        InputManager.Instance.OnConsumablePressed += HandleConsumablePressed;
        _subscribed = true;
    }

    /// <summary>
    /// Uses the consumable in the pressed slot. The keys fire in Gameplay input mode, which includes the Hub,
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
    /// Adds a charm or consumable. A new charm is equipped automatically if a charm slot is free.
    /// Returns false if the item is null or not a charm or consumable, if the charm is already owned or every charm
    /// is owned, or if the consumables are at MaxConsumables. Treasure chests and the shop call this.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public bool TryAddItem(IInventoryItem item)
    {
        switch (item)
        {
            case Charm charm:
                if (_ownedCharms.Contains(charm) || _ownedCharms.Count >= _totalCharms)
                    return false;

                _ownedCharms.Add(charm);
                EquipCharm(charm);
                break;

            case Consumable consumable:
                if (_consumables.Count >= _maxConsumables)
                    return false;

                _consumables.Add(consumable);
                break;

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

    /// <summary>Unequips every charm, removing its modifiers and ability, then removes every owned charm.</summary>
    public void ClearCharms()
    {
        for (int i = _equippedCharms.Count - 1; i >= 0; i--)
        {
            UnequipCharm(_equippedCharms[i]);
        }

        _ownedCharms.Clear();
        OnInventoryChanged?.Invoke();
    }

    /// <summary>Removes every consumable.</summary>
    public void ClearConsumables()
    {
        _consumables.Clear();
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Discards the consumable at a slot without using it, and puts the replacement in that slot, so it keeps the same
    /// key. Returns false if the slot is empty or the replacement is null. ChestLootScreen calls this when the slots
    /// are full.
    /// </summary>
    /// <param name="index">The slot, 0 to 2.</param>
    public bool ReplaceConsumable(int index, Consumable replacement)
    {
        if (replacement == null || index < 0 || index >= _consumables.Count) return false;

        _consumables[index] = replacement;
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Sets the crystal total directly and fires OnCrystalsChanged. PersistentProgress.LoadInto uses it.</summary>
    /// <param name="amount">The new total.</param>
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

    /// <summary>Removes crystals and fires OnCrystalsChanged. Returns false and spends nothing if there are too few.</summary>
    public bool TrySpendCrystals(int amount)
    {
        if (amount > _crystals)
            return false;

        _crystals -= amount;
        OnCrystalsChanged?.Invoke(_crystals);
        return true;
    }

    /// <summary>
    /// Equips an owned charm, registers its ability, and applies its modifiers with the charm as the source.
    /// Returns false if the charm is null, isn't owned, is already equipped, or every equip slot is full.
    /// </summary>
    public bool EquipCharm(Charm charm)
    {
        if (charm == null) return false;
        if (!_ownedCharms.Contains(charm)) return false;
        if (_equippedCharms.Contains(charm)) return false;
        if (_equippedCharms.Count >= _equippedCharmSlots) return false;

        _equippedCharms.Add(charm);
        AddCharmAbility(charm);

        foreach (StatModifier mod in charm.Modifiers)
        {
            ApplyModifier(mod, charm);
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Unequips a charm and removes its ability and every modifier it added, from both Stats.</summary>
    public void UnequipCharm(Charm charm)
    {
        if (!_equippedCharms.Remove(charm)) return;

        RemoveModifiers(charm);
        RemoveCharmAbility(charm);
        OnInventoryChanged?.Invoke();
    }

    private void AddCharmAbility(Charm charm)
    {
        if (_playerRoot != null)
            _playerRoot.AddAbility(charm);
    }

    private void RemoveCharmAbility(Charm charm)
    {
        if (_playerRoot != null && _playerRoot.PlayerAbilities != null)
            _playerRoot.PlayerAbilities.Remove(charm);
    }

    /// <summary>
    /// Uses the consumable at a slot: grants its ability and modifiers for its duration, heals, and takes it out of
    /// the list. ConsumableSlotsUI updates from OnInventoryChanged.
    /// </summary>
    /// <param name="index">The slot, 0 to 2.</param>
    public void UseConsumable(int index)
    {
        if (index < 0 || index >= _consumables.Count) return;

        Consumable consumable = _consumables[index];

        if (_playerRoot != null && _playerRoot.PlayerAbilities != null)
            _playerRoot.PlayerAbilities.Add(consumable, consumable.Duration);

        // A timed use gets its own source object, so using the same potion twice doesn't end the first one early
        // when RemoveAfter removes by source.
        object source = consumable.Duration > 0f ? new object() : consumable;

        foreach (StatModifier mod in consumable.Modifiers)
        {
            ApplyModifier(mod, source);
        }

        Health health = ResolveHealth();
        if (consumable.InstantHeal > 0 && health != null)
            health.RegenHealth(consumable.InstantHeal);

        // Mark consumable modifiers Permanent, so Stats doesn't remove them before this does.
        if (consumable.Duration > 0f && consumable.Modifiers.Count > 0)
            StartCoroutine(RemoveAfter(source, consumable.Duration, consumable));

        if(_consumableUseParticles != null)
            Instantiate(_consumableUseParticles, transform.position, Quaternion.identity);
        
        _consumables.RemoveAt(index);
        OnInventoryChanged?.Invoke();
        OnConsumableUsed?.Invoke(consumable);
    }

    /// <summary>Waits in game time, so pausing pauses the timer, then removes one use's modifiers.</summary>
    private IEnumerator RemoveAfter(object source, float seconds, Consumable consumable)
    {
        yield return new WaitForSeconds(seconds);
        RemoveModifiers(source);
        OnConsumableEnded?.Invoke(consumable);
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

    /// <summary>Debug: adds any item asset dragged into the field.</summary>
    [Button, DisableInEditorMode]
    private void DEBUGAddItem(ScriptableObject item)
    {
        if (item is not IInventoryItem inventoryItem)
        {
            Debug.LogWarning($"{(item != null ? item.name : "null")} isn't an IInventoryItem.", this);
            return;
        }

        TryAddItem(inventoryItem);
    }

    [Button, DisableInEditorMode]
    private void Motherlode(int amount)
    {
        SetCrystals(amount);
    }
}