using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random; 
public class Gun : MonoBehaviour
{
    [SerializeField] private GunPart[] _gunParts;

    private Dictionary<GunPartType, GunSocket> _gunSockets;
    private Dictionary<GunPartType, GameObject> _partModels;
    private Dictionary<GunPartType, GunPart> _equippedParts;


    public Stats Stats;
    public Transform Muzzle;
    public event Action OnFired;
    public event Action<DamageInfo> OnDamageDealt;
    public event Action OnPartsChanged;

    [SerializeField] private Projectile _projectilePrefab; 
    [SerializeField] private PlayerRoot _player;            

    
    [Header("Stats")]                                                       
    [SerializeField] private StatDefinition _damageStat;                       
    [SerializeField] private StatDefinition _fireRateStat;                  
    [SerializeField] private StatDefinition _rangeStat;                         
    [SerializeField] private StatDefinition _spreadStat;                     
    [SerializeField] private StatDefinition _bulletSpeedStat;                 
    [SerializeField] private StatDefinition _fireTypeStat;                    
    [SerializeField] private StatDefinition _ammoTypeStat;                    

    
    private const float DefaultDamage = 10f;      
    private const float DefaultFireRate = 4f;     
    private const float DefaultRange = 100f;      
    private const float DefaultSpread = 2f;       
    private const float DefaultBulletSpeed = 40f;

    
    private IFireMode _fireMode = new SemiAutoFire();   
    private FireMode _currentFireMode = FireMode.Semi;  
    private bool _triggerPressed;                      
    private bool _triggerHeld;                        

    public FireMode CurrentFireMode => _currentFireMode; 

   
    public float Range => ReadStat(_rangeStat, DefaultRange);                               
    public float BulletRadius => _projectilePrefab != null ? _projectilePrefab.Radius : 0f;   

    private void Awake()
    {
        _gunSockets = new Dictionary<GunPartType, GunSocket>();
        _partModels = new Dictionary<GunPartType, GameObject>();
        _equippedParts = new Dictionary<GunPartType, GunPart>();

        if (!_player) _player = GetComponentInParent<PlayerRoot>(); // CHANGED

        
    }

    private void Start()
    {
        if (Stats != null) Stats.OnStatChanged += HandleStatChanged;
        BuildGun();
        RefreshFireMode();
    }

   
    private void OnDestroy()
    {
        if (Stats != null) Stats.OnStatChanged -= HandleStatChanged;
    }

    // Builds/Rebuilds the gun using the set default frame and initial parts
    public void BuildGun()
    {
        ClearGun();

        // Only allows one socket per type to be registered
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

    public bool HasSocket(GunPartType type)
    {
        return _gunSockets.ContainsKey(type);
    }

    public GunPart GetEquippedPart(GunPartType type)
    {
        _equippedParts.TryGetValue(type, out GunPart part);

        return part;
    }

    
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

        
        if (_player != null)                 
        {                                      
            _player.ApplyModifiers(part);      
            _player.AddAbility(part);           
        }                                      

        RefreshFireMode(); 

        OnPartsChanged?.Invoke();

        return true;
    }


    [Button, DisableInEditorMode]
    public bool RemovePart(GunPartType type)
    {
        if (!_equippedParts.TryGetValue(type, out GunPart part))
        {
            return false;
        }

        RemovePartModel(part);

        _equippedParts.Remove(type);

        if (_player != null)                    
        {                                      
            _player.RemoveModifiers(part);      
            _player.RemoveAbility(part);        
        }                                       

        RefreshFireMode(); 

        OnPartsChanged?.Invoke();

        return true;
    }

    
    public void PullTrigger()
    {
        _triggerPressed = true;
        _triggerHeld = true;
    }

  
    public void ReleaseTrigger()
    {
        _triggerHeld = false;
    }

    
    public bool TryFire(AimSolution aim)
    {
        bool pressed = _triggerPressed;
        _triggerPressed = false;

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
            DamageSource = _player != null ? _player.gameObject : gameObject,
            DamageDirection = rotation * Vector3.forward,
            DamageType = GetDamageType()
        };

        Projectile projectile = Instantiate(_projectilePrefab, aim.Origin, rotation);
        projectile.Initialize(damageInfo, bulletSpeed, range, info => OnDamageDealt?.Invoke(info));

        OnFired?.Invoke();
        return true;
    }

    private void RefreshFireMode()
    {
        FireMode mode = FireMode.Semi;

        if (Stats != null && _fireTypeStat != null && Stats.HasStat(_fireTypeStat))
        {
            mode = (FireMode)Mathf.RoundToInt(Stats.GetValue(_fireTypeStat));
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

   
    private DamageType GetDamageType()
    {
        if (Stats != null && _ammoTypeStat != null && Stats.HasStat(_ammoTypeStat))
            return (DamageType)Mathf.RoundToInt(Stats.GetValue(_ammoTypeStat));

        if (GetEquippedPart(GunPartType.Magazine) is GunMagazine magazine)
            return magazine.DamageType;

        return DamageType.Physical;
    }

  
    private void HandleStatChanged(StatDefinition stat, float value)
    {
        if (stat == _fireTypeStat) RefreshFireMode();
    }

    
    private float ReadStat(StatDefinition stat, float fallback)
    {
        if (Stats == null || stat == null || !Stats.HasStat(stat))
            return fallback;

        return Stats.GetValue(stat);
    }

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

        // Spawns the part at the socket then resets its position and rotation
        GameObject model = Instantiate(part.PartPrefab, socket.transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        _partModels[part.PartType] = model;

        if (part.PartType == GunPartType.Barrel)
        {
            Transform barrelMuzzle = model.transform.Find("Muzzle");
            if (barrelMuzzle) Muzzle = barrelMuzzle;
        }
    }

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

    // Clears the guns parts and sockets
    private void ClearGun()
    {
        foreach (GameObject model in _partModels.Values)
        {
            if (model)
            {
                Destroy(model);
            }
        }

        // removes the modifiers and abilities of the parts being cleared, so rebuilding doesn't stack them.
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