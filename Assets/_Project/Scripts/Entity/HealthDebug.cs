#if UNITY_EDITOR || DEVELOPMENT_BUILD

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Provides debugging controls for health, damage,
/// healing, status effects, and invincibility.
/// </summary>
public class HealthDebug : MonoBehaviour
{
    [Header("Instant heal")]
    [SerializeField] private int _heal;
    
    [Header("Instant damage")]
    [SerializeField] private int _damage;
    
    [Header("Heal over time")]
    [SerializeField] private int _healOverTime;
    [SerializeField] private float _healTimer;

    [Header("Damage over time")]
    [SerializeField] private int _damageOverTime;
    [SerializeField] private float _damageTimer;
    
    [Header("Invincibility")]
    [SerializeField] private bool _invincibilityEnabled;

    [Space(5)] [SerializeField] private bool _apply;
    
    [SerializeField] private Health _health;
    [SerializeField] private DamageReceiver _damageReceiver;
    [SerializeField] private PlayerMovement _playerMovement;
    
    [Header("Player Debug Controls")]
    [TextArea(6, 8)]
    [SerializeField] private string _instructions =
        "F1 | Heal 25 HP\n" +
        "F2 | Deal 25 damage\n" +
        "F3 | Toggle damage over time (50 damage / 10s)\n" +
        "F4 | Toggle heal over time (50 HP / 10s)\n" +
        "F5 | Toggle Invincibility(doesn't need apply)";
    
    private object _invincibilityToken;
    private bool _invincibilityActive;
    
    private float _dotTimeRemaining; // Damage over time
    private float _hotTimeRemaining; // Heal over time

    private void Awake()
    {
        if (!_health)
            _health = GetComponent<Health>();
        if (!_playerMovement)
            _playerMovement = GetComponent<PlayerMovement>();
        if (!_damageReceiver)
            _damageReceiver = GetComponent<DamageReceiver>();
    }
    
    // Releases the debug invincibility token when disabled
    private void OnDisable()
    {
        if (_invincibilityActive && _health)
        {
            _health.StopInvincibility(_invincibilityToken);
        }

        _invincibilityToken = null;
        _invincibilityActive = false;
        _invincibilityEnabled = false;
    }
    
    private void Update()
    {
        if (!_health)
            return;
        
        AnyEntityDebug();
        PlayerDebug();
        UpdateDebugTimers();
        UpdateInvincibility();
    }

    /// <summary>
    /// Handles keyboard shortcuts for player health debugging.
    /// </summary>
    private void AnyEntityDebug()
    {
        if (_heal > 0f && _apply)
        {
            _health.RegenHealth(_heal);
            _heal = 0;
            _apply = false;
        }

        if (_damage > 0f && _apply && _damageReceiver)
        {
            _damageReceiver.TakeDamage(new DamageInfo { 
                DamageAmount = _damage, 
                DamageSource = gameObject, 
                DamageDirection = transform.forward,
                DamageType = DamageType.Physical,
            });
            
            _damage = 0;
            _apply = false;
        }

        if (_damageOverTime > 0f && _damageTimer > 0f && _apply)
        {
            StartDebugDoT(_damageOverTime, _damageTimer);
            _damageOverTime = 0;
            _damageTimer = 0f;
            _apply = false;
        }

        if (_healOverTime > 0f && _healTimer > 0f && _apply)
        {
            StartDebugHoT(_healOverTime, _healTimer);
            _healOverTime = 0;
            _healTimer = 0f;
            _apply = false;
        }
    }
    
    /// <summary>
    /// Handles keyboard shortcuts for player health debugging.
    /// </summary>
    private void PlayerDebug()
    {
        if(!_playerMovement || Keyboard.current == null)
            return;
        
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            _health.RegenHealth(25);
        }

        if (Keyboard.current.f2Key.wasPressedThisFrame && _damageReceiver)
        {
            _damageReceiver.TakeDamage(new DamageInfo { 
                DamageAmount = 25, 
                DamageSource = gameObject, 
                DamageDirection = transform.forward,
                DamageType = DamageType.Physical,
            });
        }

        if (Keyboard.current.f3Key.wasPressedThisFrame && _damageReceiver)
        {
            if (_dotTimeRemaining <= 0f)
            {
                StartDebugDoT(50, 10f);
            }
            else
            {
                _damageReceiver.TakeDamageOverTime(new DamageInfo { DamageAmount = 0 }, 0f);

                _dotTimeRemaining = 0f;
            }
        }

        if (Keyboard.current.f4Key.wasPressedThisFrame)
        {
            if (_hotTimeRemaining <= 0f)
            {
                StartDebugHoT(50, 10f);
            }
            else
            {
                _health.RegenHealthOverTime(0, 0f);
                _hotTimeRemaining = 0f;
            }
        }

        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            _invincibilityEnabled = !_invincibilityEnabled;
        }
    }
    
    private void UpdateDebugTimers()
    {
        if (_dotTimeRemaining > 0f)
            _dotTimeRemaining = Mathf.Max(0f, _dotTimeRemaining - Time.deltaTime);

        if (_hotTimeRemaining > 0f)
            _hotTimeRemaining = Mathf.Max(0f, _hotTimeRemaining - Time.deltaTime);
    }
    
    private void UpdateInvincibility()
    {
        if (_invincibilityEnabled == _invincibilityActive)
            return;

        if (_invincibilityEnabled)
        {
            _invincibilityToken = _health.StartInvincibility();
        }
        else
        {
            _health.StopInvincibility(_invincibilityToken);
            _invincibilityToken = null;
        }

        _invincibilityActive = _invincibilityEnabled;
    }
    
    // Damage over time
    private void StartDebugDoT(int damage, float duration)
    {
        if(!_damageReceiver)
            return;
        
        _damageReceiver.TakeDamageOverTime(new DamageInfo { 
                DamageAmount = damage, 
                DamageSource = gameObject, 
                DamageDirection = transform.forward,
                DamageType = DamageType.Physical,
        }, duration);
        
        _dotTimeRemaining = duration;
    }
    
    //Heal over time
    private void StartDebugHoT(int healing, float duration)
    {
        _health.RegenHealthOverTime(healing, duration);
        _hotTimeRemaining = duration;
    }
}
#endif