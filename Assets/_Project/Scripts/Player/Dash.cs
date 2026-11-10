using System;
using ScriptableSettings;
using UnityEngine;

public class Dash : MonoBehaviour
{
    [SerializeField] private DashSettings _settings;

    [Header("References")]
    // Reference direction used when dashing without movement input, usually camera orientation.
    [SerializeField] private Transform _orientation;
    [SerializeField] private Health _health;

   
    [Header("Stats")]                                                  
    [SerializeField] private StatDefinition _dashDistanceStat;          
    [SerializeField] private StatDefinition _dashDurationStat;          
    [SerializeField] private StatDefinition _dashCooldownStat;         
    [SerializeField] private StatDefinition _dashInvincibilityStat;    

    public bool IsDashing { get; private set; }
    public Vector3 DashDirection { get; private set; }

   
    public float DashDistance => ReadStat(_dashDistanceStat, _settings.DashDistance);                
    public float DashDuration => ReadStat(_dashDurationStat, _settings.DashDuration);                
    public float InvincibilityDuration => ReadStat(_dashInvincibilityStat, _settings.InvincibilityDuration); 

   
    public float DashCooldown => ReadStat(_dashCooldownStat, _settings.DashCooldown) + DashDuration; 

    public event Action OnDashed;

    private float _dashTimer;
    private float _nextDashTime;
    private bool _intendToDash;

    // Tracks this dash's invincibility separately from other sources.
    private object invincibilityToken;
    private bool _wasInvincible = false;

    [SerializeField] private Stats _playerStats;

    public void SetStats(Stats playerStats)
    {
        if (_playerStats == null)
        {
            _playerStats = playerStats;
        }
    }

    private void Awake()
    {
        if (!_orientation)
            Debug.LogError($"{nameof(Dash)} requires a {nameof(_orientation)}");

        if (!_health)
            _health = GetComponent<Health>();

        
    }

    private void OnDisable()
    {
        CancelDash();
    }

    /// <summary>
    /// Calculates dash velocity using a linear slowdown over its duration.
    /// The initial speed is doubled to maintain the configured dash distance.
    /// </summary>
    public Vector3 GetDashVelocity()
    {
        float dashSpeed = DashDistance / DashDuration; 

        return DashDirection * dashSpeed;
    }

    /// <summary>
    /// Updates an active dash or starts a requested dash if allowed.
    /// Dash direction is locked at the start and cooldown includes dash duration.
    /// </summary>
    public void HandleDash(Vector3 direction, bool canDash = true)
    {
        if (IsDashing)
        {
            _intendToDash = false;

            _dashTimer += Time.deltaTime;

            if (_dashTimer >= DashDuration) 
            {
                CancelDash();
                IsDashing = false;
            }

            HandleInvincibility();
            return;
        }

        if (!_intendToDash)
            return;

        _intendToDash = false;

        if (!canDash || Time.time < _nextDashTime)
            return;

        // If there is no movement input dash to camera direction
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = _orientation.transform.forward;
            direction.y = 0f;
        }

        DashDirection = direction.normalized;
        _dashTimer = 0f;
        IsDashing = true;

        //Cooldown
        _nextDashTime = Time.time + DashCooldown; 

        OnDashed?.Invoke();
    }

    /// <summary>
    /// Stops the dash and removes only its own invincibility.
    /// </summary>
    public void CancelDash()
    {
        if (!IsDashing)
            return;

        IsDashing = false;
        StopDashInvincibility();
        _wasInvincible = false;
    }

    /// <summary>
    /// Enables invincibility only during the configured window of the dash.
    /// Tracks state changes to avoid repeatedly creating or removing tokens.
    /// </summary>
    private void HandleInvincibility()
    {
        if (!_health || !_settings.InvincibilityEnabled)
            return;

        bool shouldBeInvincible = IsDashing &&
            _dashTimer >= _settings.InvincibilityStart &&
            _dashTimer < _settings.InvincibilityStart + InvincibilityDuration; // CHANGED: reads the stat

        if (shouldBeInvincible == _wasInvincible)
            return;

        if (shouldBeInvincible)
            StartDashInvincibility();
        else
            StopDashInvincibility();

        _wasInvincible = shouldBeInvincible;
    }

    private void StartDashInvincibility()
    {
        if (invincibilityToken != null || !_health || !_settings.InvincibilityEnabled)
            return;

        invincibilityToken = _health.StartInvincibility();
    }

    private void StopDashInvincibility()
    {
        if (!_health || !_settings.InvincibilityEnabled)
            return;

        _health.StopInvincibility(invincibilityToken);
        invincibilityToken = null;
    }

    public void IntendToDash() => _intendToDash = true;

    
    private float ReadStat(StatDefinition stat, float fallback)
    {
        if (_playerStats == null || stat == null || !_playerStats.HasStat(stat))
            return fallback;

        return _playerStats.GetValue(stat);
    }
}