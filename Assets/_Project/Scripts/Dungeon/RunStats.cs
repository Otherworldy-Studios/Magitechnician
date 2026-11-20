using _Project.Scripts.Dungeon;
using UnityEngine;

/// <summary>
/// Counts the current run's stats for the death and win screens. GameManager owns one, calls Begin when a run starts,
/// and calls Stop when the run ends or the scene changes.
/// </summary>
public class RunStats
{
    private Health _health;
    private Gun _gun;
    private Inventory _inventory;
    private WaveManager _waveManager;
    private int _lastCrystals; // The crystal total at the last change, to tell gains from spending.
    private bool _isCounting;

    /// <summary>Damage that got through to the player's health. Damage the shield absorbed isn't counted.</summary>
    public int DamageTaken { get; private set; }
    /// <summary>Damage the player's gun dealt.</summary>
    public int DamageDealt { get; private set; }
    public int EnemiesKilled { get; private set; }
    /// <summary>Crystals gained during the run. Spending doesn't lower it.</summary>
    public int CrystalsCollected { get; private set; }
    public int RoomsCleared { get; private set; }

    /// <summary>Resets every stat and starts listening to the player and the WaveManager.</summary>
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

        if (player == null)
        {
            Debug.LogError("RunStats.Begin needs the player's PlayerRoot.");
            return;
        }

        _health = player.PlayerHealth;
        _gun = player.Gun;
        _inventory = player.Inventory;
        _waveManager = waveManager;

        if (_health != null) _health.OnDamageInstance += HandleDamageTaken;
        if (_gun != null) _gun.OnDamageDealt += HandleDamageDealt;
        if (_waveManager != null) _waveManager.OnEnemyKilled += HandleEnemyKilled;

        if (_inventory != null)
        {
            _lastCrystals = _inventory.Crystals;
            _inventory.OnCrystalsChanged += HandleCrystalsChanged;
        }

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
        if (_inventory != null) _inventory.OnCrystalsChanged -= HandleCrystalsChanged;
        Room.OnRoomCleared -= HandleRoomCleared;

        _health = null;
        _gun = null;
        _inventory = null;
        _waveManager = null;
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

    private void HandleRoomCleared(Room room)
    {
        RoomsCleared++;
    }
}