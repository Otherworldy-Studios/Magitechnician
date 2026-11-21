using _Project.Scripts.Dungeon;
using UnityEngine;

/// <summary>
/// Counts the current run's stats for the death and win screens: damage, kills, crystals, rooms, charm activations,
/// consumables used, and the average time per room. GameManager owns one, calls Begin when a run starts, and calls
/// Stop when the run ends or the scene changes.
/// </summary>
public class RunStats
{
    private Health _health;
    private Gun _gun;
    private Inventory _inventory;
    private PlayerAbilities _abilities;
    private WaveManager _waveManager;
    private int _lastCrystals;    // The crystal total at the last change, to tell gains from spending.
    private Room _timedRoom;      // The uncleared room the player is in, timed until it's cleared.
    private float _roomEnterTime; // Time.time when the player entered _timedRoom.
    private float _totalRoomTime; // Seconds spent clearing rooms, added up.
    private int _timedRoomCount;  // Rooms counted in _totalRoomTime.
    private bool _isCounting;

    /// <summary>Damage that got through to the player's health. Damage the shield absorbed isn't counted.</summary>
    public int DamageTaken { get; private set; }
    /// <summary>Damage the player's gun dealt.</summary>
    public int DamageDealt { get; private set; }
    public int EnemiesKilled { get; private set; }
    /// <summary>Crystals gained during the run. Spending doesn't lower it.</summary>
    public int CrystalsCollected { get; private set; }
    public int RoomsCleared { get; private set; }
    /// <summary>Times an equipped charm's ability did its effect, such as Leech Rounds restoring shield.</summary>
    public int CharmActivations { get; private set; }
    public int ConsumablesUsed { get; private set; }

    /// <summary>
    /// Average seconds from entering a room to clearing it. Paused time, including the reward screen, isn't counted.
    /// 0 until a room is cleared.
    /// </summary>
    public float AverageRoomTime => _timedRoomCount > 0 ? _totalRoomTime / _timedRoomCount : 0f;

    /// <summary>Resets every stat and starts listening to the player, the WaveManager, and rooms.</summary>
    /// <param name="player">The run's player.</param>
    /// <param name="waveManager">The Level's WaveManager, for kills. Can be null, which leaves kills at 0.</param>
    public void Begin(PlayerRoot player, WaveManager waveManager)
    {
        Stop();

        DamageTaken = 0;
        DamageDealt = 0;
        EnemiesKilled = 0;
        CrystalsCollected = 0;
        RoomsCleared = 0;
        CharmActivations = 0;
        ConsumablesUsed = 0;
        _timedRoom = null;
        _totalRoomTime = 0f;
        _timedRoomCount = 0;

        if (player == null)
        {
            Debug.LogError("RunStats.Begin needs the player's PlayerRoot.");
            return;
        }

        _health = player.PlayerHealth;
        _gun = player.Gun;
        _inventory = player.Inventory;
        _abilities = player.PlayerAbilities;
        _waveManager = waveManager;

        if (_health != null) _health.OnDamageInstance += HandleDamageTaken;
        if (_gun != null) _gun.OnDamageDealt += HandleDamageDealt;
        if (_waveManager != null) _waveManager.OnEnemyKilled += HandleEnemyKilled;
        if (_abilities != null) _abilities.OnAbilityActivated += HandleAbilityActivated;

        if (_inventory != null)
        {
            _lastCrystals = _inventory.Crystals;
            _inventory.OnCrystalsChanged += HandleCrystalsChanged;
            _inventory.OnConsumableUsed += HandleConsumableUsed;
        }

        GameManager.Instance.OnRoomEntered += HandleRoomEntered;
        Room.OnRoomCleared += HandleRoomCleared;
        _isCounting = true;
    }

    /// <summary>Stops listening. The stats keep their values, so the death and win screens can read them.</summary>
    public void Stop()
    {
        if (!_isCounting) return;

        if (_health != null) _health.OnDamageInstance -= HandleDamageTaken;
        if (_gun != null) _gun.OnDamageDealt -= HandleDamageDealt;
        if (_waveManager != null) _waveManager.OnEnemyKilled -= HandleEnemyKilled;
        if (_abilities != null) _abilities.OnAbilityActivated -= HandleAbilityActivated;

        if (_inventory != null)
        {
            _inventory.OnCrystalsChanged -= HandleCrystalsChanged;
            _inventory.OnConsumableUsed -= HandleConsumableUsed;
        }

        if (GameManager.HasInstance) GameManager.Instance.OnRoomEntered -= HandleRoomEntered;
        Room.OnRoomCleared -= HandleRoomCleared;

        _health = null;
        _gun = null;
        _inventory = null;
        _abilities = null;
        _waveManager = null;
        _timedRoom = null;
        _isCounting = false;
    }

    private void HandleDamageTaken(DamageInfo damageInfo)
    {
        DamageTaken += damageInfo.DamageAmount;
    }

    private void HandleDamageDealt(GameObject target, DamageInfo damageInfo)
    {
        DamageDealt += damageInfo.DamageAmount;
    }

    private void HandleEnemyKilled(EnemyBase enemy)
    {
        EnemiesKilled++;
    }

    private void HandleCrystalsChanged(int total)
    {
        if (total > _lastCrystals)
        {
            CrystalsCollected += total - _lastCrystals;
        }

        _lastCrystals = total;
    }

    private void HandleConsumableUsed(Consumable consumable)
    {
        ConsumablesUsed++;
    }

    // Only counts abilities that came from a charm, because gun parts and consumables have abilities too.
    private void HandleAbilityActivated(IInventoryItem sourceItem)
    {
        if (sourceItem is Charm)
        {
            CharmActivations++;
        }
    }

    /// <summary>Starts timing the room, unless it's already cleared, such as when the player walks back through.</summary>
    private void HandleRoomEntered(Room room)
    {
        if (room == null || room.Complete)
        {
            _timedRoom = null;
            return;
        }

        _timedRoom = room;
        _roomEnterTime = Time.time;
    }

    private void HandleRoomCleared(Room room)
    {
        RoomsCleared++;

        if (room != _timedRoom) return;

        _totalRoomTime += Time.time - _roomEnterTime;
        _timedRoomCount++;
        _timedRoom = null;
    }
}