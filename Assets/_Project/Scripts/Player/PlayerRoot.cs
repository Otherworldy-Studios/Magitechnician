using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The player's hub. Goes on the player prefab's root. Holds references to the player's components, hands the player's
/// Stats to each of them, registers the player with GameManager, and routes item modifiers to the player's or the
/// gun's Stats.
/// </summary>
// INTEGRATION: new. Before, each player script found its own references and no script owned the player as a whole.
// GameManager.RegisterPlayer, TreasureChest, RunStartPortal, InteractionDetector, and Gun all reach the player through it.
public class PlayerRoot : MonoBehaviour
{
    // Each is found in children if left empty.
    [SerializeField] private Health _health;
    [SerializeField] private ShieldSystem _shield;
    [SerializeField] private Dash _dash;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private Stats _playerStats; // The player's Stats, not the gun's. See FindPlayerStats.
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private Gun _gun;

    /// <summary>The player's Health. GameManager listens to its OnDied to end the run.</summary>
    public Health PlayerHealth => _health;
    /// <summary>The player's shield.</summary>
    public ShieldSystem Shield => _shield;
    /// <summary>The player's dash.</summary>
    public Dash PlayerDash => _dash;
    /// <summary>The player's inventory. TreasureChest and the shop add items through it.</summary>
    public Inventory Inventory => _inventory;
    /// <summary>The player's Stats, which hold max health, speed, dash, and shield stats.</summary>
    public Stats PlayerStats => _playerStats;
    /// <summary>The player's movement.</summary>
    public PlayerMovement PlayerMovement => _playerMovement;
    /// <summary>The player's gun. Its own Stats hold damage, fire rate, range, spread, and bullet speed.</summary>
    public Gun Gun => _gun;

    /// <summary>Finds any references left empty, then hands the player's Stats to each component.</summary>
    private void Awake()
    {
        // The gun is found first, so FindPlayerStats can skip the gun's Stats.
        if (!_gun)
        {
            _gun = GetComponentInChildren<Gun>();
        }
        if (!_playerStats)
        {
            _playerStats = FindPlayerStats();
        }
        if (!_health)
        {
            _health = GetComponentInChildren<Health>();
        }

        if (!_shield)
        {
            _shield = GetComponentInChildren<ShieldSystem>();
        }

        if (!_dash)
        {
            _dash = GetComponentInChildren<Dash>();
        }

        if (!_inventory)
        {
            _inventory = GetComponentInChildren<Inventory>();
        }
        if (!_playerMovement)
        {
            _playerMovement = GetComponentInChildren<PlayerMovement>();
        }
        AssignStats();
    }

    /// <summary>
    /// Calls SetStats on Health, ShieldSystem, Dash, Inventory, and PlayerMovement, so they all read the same Stats.
    /// Also gives Inventory the gun's Stats, for charms that change gun stats. Logs a warning for each missing reference.
    /// </summary>
    // Runs in Awake, so every SetStats happens before any Start, where Health and ShieldSystem read their max values.
    private void AssignStats()
    {
        if (!_playerStats)
        {
            Debug.LogWarning($"{nameof(PlayerRoot)} on {name} cannot assign stats because no {nameof(Stats)} reference was found.", this);
            return;
        }

        if (_health)
        {
            _health.SetStats(_playerStats);
        }
        else
        {
            Debug.LogWarning($"{nameof(PlayerRoot)} on {name} is missing a {nameof(Health)} reference.", this);
        }

        if (_shield)
        {
            _shield.SetStats(_playerStats);
        }
        else
        {
            Debug.LogWarning($"{nameof(PlayerRoot)} on {name} is missing a {nameof(ShieldSystem)} reference.", this);
        }

        if (_dash)
        {
            _dash.SetStats(_playerStats);
        }
        else
        {
            Debug.LogWarning($"{nameof(PlayerRoot)} on {name} is missing a {nameof(Dash)} reference.", this);
        }

        if (_inventory)
        {
            _inventory.SetStats(_playerStats);
            if (_gun) _inventory.SetGunStats(_gun.Stats);
        }
        else
        {
            Debug.LogWarning($"{nameof(PlayerRoot)} on {name} is missing an {nameof(Inventory)} reference.", this);
        }

        if (_playerMovement)
        {
            _playerMovement.SetStats(_playerStats);
        }
        else
        {
            Debug.LogWarning($"{nameof(PlayerRoot)} on {name} is missing a {nameof(PlayerMovement)} reference.", this);
        }

        if (!_gun)
        {
            Debug.LogWarning($"{nameof(PlayerRoot)} on {name} is missing a {nameof(Gun)} reference.", this);
        }
    }

    /// <summary>
    /// Registers this player with GameManager, which listens for death, loads saved progress, and, in the Dungeon,
    /// spawns the player and requests generation.
    /// </summary>
    private void Start()
    {
        GameManager.Instance.RegisterPlayer(this);
    }

    /// <summary>The first Stats in children that isn't on the gun or under it. Null if there's none.</summary>
    private Stats FindPlayerStats()
    {
        foreach (Stats stats in GetComponentsInChildren<Stats>(true))
        {
            if (_gun != null && stats == _gun.Stats) continue;
            if (stats.GetComponentInParent<Gun>() != null) continue;
            return stats;
        }

        return null;
    }

    /// <summary>
    /// Adds every modifier on the item, with the item as the source. Each modifier goes to whichever Stats has its stat:
    /// the player's for max health, speed, and dash and shield stats, and the gun's for damage, fire rate, range,
    /// spread, and bullet speed. So any item can change any stat. Gun.EquipPart calls this.
    /// </summary>
    public void ApplyModifiers(IInventoryItem item)
    {
        List<StatModifier> modifiers = GetModifiers(item);
        if (modifiers == null) return;

        Stats gunStats = _gun != null ? _gun.Stats : null;

        foreach (StatModifier modifier in modifiers)
        {
            if (modifier == null || modifier.Stat == null) continue;

            if (_playerStats != null && _playerStats.HasStat(modifier.Stat))
                _playerStats.AddModifier(modifier, item);
            else if (gunStats != null && gunStats.HasStat(modifier.Stat))
                gunStats.AddModifier(modifier, item);
            else
                Debug.LogWarning($"{modifier.Stat.name} isn't on the player's or the gun's StatBlock. Modifier from {item.DisplayName} skipped.", this);
        }
    }

    /// <summary>Removes everything the item added, from both Stats. Gun.RemovePart calls this.</summary>
    public void RemoveModifiers(IInventoryItem item)
    {
        if (item == null) return;

        if (_playerStats != null) _playerStats.RemoveAllFromSource(item);
        if (_gun != null && _gun.Stats != null) _gun.Stats.RemoveAllFromSource(item);
    }

    /// <summary>
    /// Starts the item's ability, if it has one. Empty until PlayerAbilities (Team 1) exists.
    /// TODO: add a PlayerAbilities field here and uncomment the line.
    /// </summary>
    public void AddAbility(IInventoryItem item)
    {
        // _abilities?.Add(item);
    }

    /// <summary>Stops the item's ability. Empty until PlayerAbilities exists.</summary>
    public void RemoveAbility(IInventoryItem item)
    {
        // _abilities?.Remove(item);
    }

    /// <summary>The modifier list of a gun part, charm, or consumable. Null for any other item.</summary>
    private static List<StatModifier> GetModifiers(IInventoryItem item)
    {
        return item switch
        {
            GunPart part => part.Modifiers,
            Charm charm => charm.Modifiers,
            Consumable consumable => consumable.Modifiers,
            _ => null
        };
    }
}
