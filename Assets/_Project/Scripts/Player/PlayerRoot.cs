using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRoot : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private ShieldSystem _shield;
    [SerializeField] private Dash _dash;
    [SerializeField] private Inventory _inventory;
    [SerializeField] private Stats _playerStats;
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private Gun _gun;

    public Health PlayerHealth => _health;
    public ShieldSystem Shield => _shield;
    public Dash PlayerDash => _dash;

    public Inventory Inventory => _inventory;

    public Stats PlayerStats => _playerStats;

    public PlayerMovement PlayerMovement => _playerMovement;

    public Gun Gun => _gun; 

    private void Awake()
    {
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
            Debug.LogWarning($"{nameof(PlayerRoot)} on {name} is missing a {nameof(Gun)} reference.", this); // CHANGED
        }

    }

    private void Start()
    {
        GameManager.Instance.RegisterPlayer(this);
    }

   
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

    // Adds every modifier on the item, with the item as the source.
    // Each modifier goes to whichever Stats has its stat: the player's for MaxHealth, Speed, and dash and shield stats,
    // the gun's for Damage, FireRate, Range, Spread, and BulletSpeed. So any item can change any stat.
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

    // Removes everything the item added, from both Stats.
    public void RemoveModifiers(IInventoryItem item)
    {
        if (item == null) return;

        if (_playerStats != null) _playerStats.RemoveAllFromSource(item);
        if (_gun != null && _gun.Stats != null) _gun.Stats.RemoveAllFromSource(item);
    }

    // Starts the item's ability, if it has one.
    // here until playerabilities is done 
    // so the project compiles. TODO: add a PlayerAbilities field here and uncomment the line.
    public void AddAbility(IInventoryItem item)
    {
        // _abilities?.Add(item);
    }


    public void RemoveAbility(IInventoryItem item)
    {
        // _abilities?.Remove(item);
    }

   
    private static List<StatModifier> GetModifiers(IInventoryItem item)
    {
        return item switch
        {
            GunPart part => part.statModifiers,
            Charm charm => charm.statModifiers,
            Consumable consumable => consumable.statModifiers,
            _ => null
        };
    }
}
