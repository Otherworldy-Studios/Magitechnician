using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

/// <summary>
/// The modular gun. Builds itself from its starting parts on its five sockets, equips and removes parts, and fires.
/// Equipping a part places its model and applies its stat modifiers through PlayerRoot; removing does the reverse.
/// Firing reads every gun stat at that moment, so parts and consumables apply on the next shot.
/// Shoot pulls and releases the trigger and calls TryFire every frame; the fire mode decides when a shot happens.
/// </summary>
public class Gun : MonoBehaviour
{
    [Tooltip("The parts the gun starts with. BuildGun equips them in Start.")]
    [SerializeField] private GunPart[] _gunParts;

    private Dictionary<GunPartType, GunSocket> _gunSockets;     // The socket for each part type, found under this object.
    private Dictionary<GunPartType, GameObject> _partModels;    // The spawned model of each equipped part.
    private Dictionary<GunPartType, GunPart> _equippedParts;    // The equipped part in each slot.

    [Tooltip("The gun's own Stats, with StatBlock_Gun. PlayerRoot.FindPlayerStats skips it.")]
    [SerializeField, FormerlySerializedAs("Stats")] private Stats _stats;
    [Tooltip("Where bullets start. Replaced by the Muzzle child of the equipped barrel's model when it has one.")]
    [SerializeField, FormerlySerializedAs("Muzzle")] private Transform _muzzle;

    /// <summary>The gun's Stats. PlayerRoot hands it to Inventory, and sends gun-stat modifiers here.</summary>
    public Stats Stats => _stats;
    /// <summary>Where bullets start. Shoot passes its position to CrossHairAim.</summary>
    public Transform Muzzle => _muzzle;

    /// <summary>Fires after each shot. Not Shoot's OnFired, which fires from the same shot.</summary>
    public event Action OnFired;
    /// <summary>Fires when one of this gun's bullets damages something. For hit markers and contested health.</summary>
    public event Action<DamageInfo> OnDamageDealt;
    /// <summary>Fires after a part is equipped or removed. For the HUD's parts strip.</summary>
    public event Action OnPartsChanged;

    [Tooltip("The bullet to spawn. Moved here from Shoot.")]
    [SerializeField] private Projectile _projectilePrefab; // INTEGRATION: new. The bullet prefab moved from Shoot to Gun.
    [Tooltip("Found in parents if empty.")]
    [SerializeField] private PlayerRoot _player;           // INTEGRATION: new. Equipping goes through PlayerRoot.ApplyModifiers.

    // INTEGRATION: new. The gun reads these stats from its own Stats. Lee chose to keep them private.
    [Header("Stats")]
    [SerializeField] private StatDefinition _damageStat;       // Stat_Damage, per bullet.
    [SerializeField] private StatDefinition _fireRateStat;     // Stat_FireRate, in shots per second.
    [SerializeField] private StatDefinition _rangeStat;        // Stat_Range, in meters.
    [SerializeField] private StatDefinition _spreadStat;       // Stat_Spread, in degrees.
    [SerializeField] private StatDefinition _bulletSpeedStat;  // Stat_BulletSpeed, in meters per second.
    [SerializeField] private StatDefinition _fireTypeStat;     // Stat_FireType, a FireMode as a number.
    [SerializeField] private StatDefinition _ammoTypeStat;     // Stat_AmmoType, a DamageType as a number.

    // Used when a stat isn't assigned or isn't in the gun's StatBlock.
    private const float DefaultDamage = 10f;
    private const float DefaultFireRate = 4f;
    private const float DefaultRange = 100f;
    private const float DefaultSpread = 2f;
    private const float DefaultBulletSpeed = 40f;

    // INTEGRATION: new. Trigger state and fire mode for TryFire.
    private IFireMode _fireMode = new SemiAutoFire();  // Decides when to fire. Replaced by RefreshFireMode.
    private FireMode _currentFireMode = FireMode.Semi; // Which mode _fireMode is, so it's only replaced on a change.
    private bool _triggerPressed;                     // True from PullTrigger until the next TryFire reads it.
    private bool _triggerHeld;                        // True from PullTrigger until ReleaseTrigger.

    /// <summary>The fire mode in use.</summary>
    public FireMode CurrentFireMode => _currentFireMode;

    /// <summary>The current range, from Stat_Range. Shoot passes it to CrossHairAim.</summary>
    public float Range => ReadStat(_rangeStat, DefaultRange);
    /// <summary>The bullet's radius, from the projectile prefab. Shoot passes it to CrossHairAim.</summary>
    public float BulletRadius => _projectilePrefab != null ? _projectilePrefab.Radius : 0f;

    /// <summary>Creates the lookup tables and finds PlayerRoot.</summary>
    private void Awake()
    {
        _gunSockets = new Dictionary<GunPartType, GunSocket>();
        _partModels = new Dictionary<GunPartType, GameObject>();
        _equippedParts = new Dictionary<GunPartType, GunPart>();

        if (!_player) _player = GetComponentInParent<PlayerRoot>();
    }

    /// <summary>Listens to stat changes, builds the gun, and picks the fire mode.</summary>
    // INTEGRATION: BuildGun moved here from Awake, so PlayerRoot.Awake has handed out the Stats before parts apply modifiers.
    private void Start()
    {
        if (_stats != null) _stats.OnStatChanged += HandleStatChanged;
        BuildGun();
        RefreshFireMode();
    }

    /// <summary>Stops listening to stat changes.</summary>
    private void OnDestroy()
    {
        if (_stats != null) _stats.OnStatChanged -= HandleStatChanged;
    }

    /// <summary>Clears the gun, finds its sockets, and equips the starting parts.</summary>
    public void BuildGun()
    {
        ClearGun();

        // Only one socket per type is registered.
        foreach (GunSocket socket in GetComponentsInChildren<GunSocket>(true))
        {
            _gunSockets.TryAdd(socket.SocketType, socket);
        }

        if (_gunParts != null)
        {
            foreach (GunPart part in _gunParts)
            {
                if (!part)
                {
                    continue;
                }

                EquipPart(part);
            }
        }
    }

    /// <summary>True when the gun has a socket for this part type.</summary>
    public bool HasSocket(GunPartType type)
    {
        return _gunSockets.ContainsKey(type);
    }

    /// <summary>Returns the part in a slot, or null if the slot is empty.</summary>
    public GunPart GetEquippedPart(GunPartType type)
    {
        _equippedParts.TryGetValue(type, out GunPart part);

        return part;
    }

    /// <summary>
    /// Equips a part, replacing the part of the same type. Places its model, applies its modifiers, and updates the
    /// fire mode. Returns false if the part is null or the gun has no socket for it. Also an Inspector button.
    /// </summary>
    /// <param name="part">The part to equip.</param>
    [Button, DisableInEditorMode]
    public bool EquipPart(GunPart part)
    {
        if (!part)
        {
            return false;
        }

        if (!HasSocket(part.PartType))
        {
            return false;
        }

        if (_equippedParts.ContainsKey(part.PartType))
        {
            RemovePart(part.PartType);
        }

        _equippedParts[part.PartType] = part;

        SpawnPartModel(part);

        // INTEGRATION: fills the "Recalculate Stats" TODO. PlayerRoot sends each modifier to the gun's or the player's Stats,
        // with the part as the source. AddAbility is empty until PlayerAbilities exists.
        if (_player != null)
        {
            _player.ApplyModifiers(part);
            _player.AddAbility(part);
        }

        RefreshFireMode(); // INTEGRATION: a new barrel can change the fire mode.

        OnPartsChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// Removes the part in a slot, its model, and its modifiers. Returns false if the slot was empty. Also an Inspector button.
    /// </summary>
    /// <param name="type">The slot to empty.</param>
    [Button, DisableInEditorMode]
    public bool RemovePart(GunPartType type)
    {
        if (!_equippedParts.TryGetValue(type, out GunPart part))
        {
            return false;
        }

        RemovePartModel(part);

        _equippedParts.Remove(type);

        // INTEGRATION: fills the "Recalculate Stats" TODO. Removes everything this part added, from both Stats.
        if (_player != null)
        {
            _player.RemoveModifiers(part);
            _player.RemoveAbility(part);
        }

        RefreshFireMode();

        OnPartsChanged?.Invoke();

        return true;
    }

    /// <summary>Marks the trigger as pressed this frame and held. Shoot calls it on InputManager.OnFirePressed.</summary>
    public void PullTrigger()
    {
        _triggerPressed = true;
        _triggerHeld = true;
    }

    /// <summary>Marks the trigger as released. Shoot calls it when fire isn't held.</summary>
    public void ReleaseTrigger()
    {
        _triggerHeld = false;
    }

    /// <summary>
    /// Fires one bullet if the fire mode allows it this frame. Reads damage, range, spread, and bullet speed from the
    /// gun's stats, turns the aim direction by a random angle within the spread, and spawns the bullet at the aim's
    /// origin. Returns true when a bullet was fired. Shoot calls it every frame.
    /// </summary>
    /// <param name="aim">This frame's aim, from CrossHairAim.Solve. Its Origin is the muzzle, or the camera when a wall blocks the muzzle.</param>
    // INTEGRATION: was a stub that returned true. Takes an AimSolution, so a blocked muzzle can't shoot through walls.
    public bool TryFire(AimSolution aim)
    {
        bool pressed = _triggerPressed;
        _triggerPressed = false; // A press only counts on the first TryFire after it.

        float fireRate = Mathf.Max(0.01f, ReadStat(_fireRateStat, DefaultFireRate));
        if (!_fireMode.ShouldFire(pressed, _triggerHeld, fireRate, Time.time))
            return false;

        if (_projectilePrefab == null)
        {
            Debug.LogWarning($"{name} needs a projectile prefab to fire.");
            return false;
        }

        float damage = ReadStat(_damageStat, DefaultDamage);
        float range = ReadStat(_rangeStat, DefaultRange);
        float spread = ReadStat(_spreadStat, DefaultSpread);
        float bulletSpeed = ReadStat(_bulletSpeedStat, DefaultBulletSpeed);

        // Spread: a random angle inside a cone of "spread" degrees around the aim direction.
        Vector2 offset = Random.insideUnitCircle * spread;
        Quaternion rotation = Quaternion.LookRotation(aim.Direction) * Quaternion.Euler(-offset.y, offset.x, 0f);

        DamageInfo damageInfo = new DamageInfo
        {
            DamageAmount = Mathf.RoundToInt(damage),
            DamageSource = _player != null ? _player.gameObject : gameObject, // The bullet skips colliders under this.
            DamageDirection = rotation * Vector3.forward,
            DamageType = GetDamageType()
        };

        // INTEGRATION: Instantiate until ProjectilePool exists. Projectile.Initialize gets the stats read above, and
        // the callback fires OnDamageDealt when the bullet hits.
        Projectile projectile = Instantiate(_projectilePrefab, aim.Origin, rotation);
        projectile.Initialize(damageInfo, bulletSpeed, range, info => OnDamageDealt?.Invoke(info));

        OnFired?.Invoke();
        return true;
    }

    /// <summary>
    /// Picks the fire mode: from Stat_FireType when the gun's StatBlock has it, otherwise from the equipped barrel.
    /// Creates a new fire mode object only when the mode changes, so its timing isn't reset every time.
    /// </summary>
    // INTEGRATION: new. Runs in Start, after each equip or removal, and when Stat_FireType changes.
    private void RefreshFireMode()
    {
        FireMode mode = FireMode.Semi;

        if (_stats != null && _fireTypeStat != null && _stats.HasStat(_fireTypeStat))
        {
            mode = (FireMode)Mathf.RoundToInt(_stats.GetValue(_fireTypeStat));
        }
        else if (GetEquippedPart(GunPartType.Barrel) is GunBarrel barrel)
        {
            mode = barrel.FireMode;
        }

        if (mode == _currentFireMode && _fireMode != null) return;

        _currentFireMode = mode;
        _fireMode = mode switch
        {
            FireMode.Burst => new BurstFire(),
            FireMode.Auto => new AutoFire(),
            _ => new SemiAutoFire()
        };
    }

    /// <summary>Picks the damage type: from Stat_AmmoType when the gun's StatBlock has it, otherwise from the magazine, otherwise Physical.</summary>
    private DamageType GetDamageType()
    {
        if (_stats != null && _ammoTypeStat != null && _stats.HasStat(_ammoTypeStat))
            return (DamageType)Mathf.RoundToInt(_stats.GetValue(_ammoTypeStat));

        if (GetEquippedPart(GunPartType.Magazine) is GunMagazine magazine)
            return magazine.DamageType;

        return DamageType.Physical;
    }

    /// <summary>Updates the fire mode when Stat_FireType changes.</summary>
    // INTEGRATION: new. Listens to Stats.OnStatChanged.
    private void HandleStatChanged(StatDefinition stat, float value)
    {
        if (stat == _fireTypeStat) RefreshFireMode();
    }

    /// <summary>Returns the stat's value, or the fallback when there's no Stats, no stat assigned, or the StatBlock doesn't list it.</summary>
    // INTEGRATION: new. The same helper every stat-reading script uses.
    private float ReadStat(StatDefinition stat, float fallback)
    {
        if (_stats == null || stat == null || !_stats.HasStat(stat))
            return fallback;

        return _stats.GetValue(stat);
    }

    /// <summary>
    /// Places a part's model on its socket, replacing the old model. For a barrel, uses the model's Muzzle child as the muzzle.
    /// </summary>
    private void SpawnPartModel(GunPart part)
    {
        RemovePartModel(part);

        if (!part.PartPrefab)
        {
            return;
        }

        if (!_gunSockets.TryGetValue(part.PartType, out GunSocket socket))
        {
            return;
        }

        // Spawns the part at the socket, then resets its position and rotation.
        GameObject model = Instantiate(part.PartPrefab, socket.transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        _partModels[part.PartType] = model;

        // INTEGRATION: new. Each barrel has its own length, so the muzzle comes from the barrel's model.
        if (part.PartType == GunPartType.Barrel)
        {
            Transform barrelMuzzle = model.transform.Find("Muzzle");
            if (barrelMuzzle) _muzzle = barrelMuzzle;
        }
    }

    /// <summary>Destroys the model of a part's slot, if there is one.</summary>
    private void RemovePartModel(GunPart part)
    {
        if (!_partModels.TryGetValue(part.PartType, out GameObject model))
        {
            return;
        }

        if (model)
        {
            Destroy(model);
        }

        _partModels.Remove(part.PartType);
    }

    /// <summary>Destroys every part model, removes the parts' modifiers, and forgets the sockets and parts.</summary>
    private void ClearGun()
    {
        foreach (GameObject model in _partModels.Values)
        {
            if (model)
            {
                Destroy(model);
            }
        }

        // INTEGRATION: removes the modifiers and abilities of the parts being cleared, so rebuilding doesn't stack them.
        if (_player != null)
        {
            foreach (GunPart part in _equippedParts.Values)
            {
                _player.RemoveModifiers(part);
                _player.RemoveAbility(part);
            }
        }
        _gunSockets.Clear();
        _partModels.Clear();
        _equippedParts.Clear();
    }
}
