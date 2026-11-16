using System;
using ScriptableSettings;
using UnityEngine;

/// <summary>
/// The player's dash. PlayerMovement passes Dash presses to IntendToDash and calls HandleDash every frame. A dash
/// moves a set distance over a set time, slowing down as it goes, with an invincibility window partway through.
/// </summary>
public class Dash : MonoBehaviour
{
    [SerializeField] private DashSettings _settings;

    [Header("References")]
    // Reference direction used when dashing without movement input, usually camera orientation.
    [SerializeField] private Transform _orientation;
    [SerializeField] private Health _health;

    // INTEGRATION: new. Which stats in _playerStats hold each dash value. The properties below read them.
    [Header("Stats")]
    [SerializeField] private StatDefinition _dashDistanceStat;
    [SerializeField] private StatDefinition _dashDurationStat;
    [SerializeField] private StatDefinition _dashCooldownStat;
    [SerializeField] private StatDefinition _dashInvincibilityStat;

    /// <summary>True during a dash. PlayerMovement uses the dash velocity instead of walking while this is true.</summary>
    public bool IsDashing { get; private set; }

    /// <summary>The direction of the current dash, locked when it starts. PlayerAnimator reads it.</summary>
    public Vector3 DashDirection { get; private set; }

    // INTEGRATION: new. Each was read straight from _settings. Now each reads its stat, falling back to _settings when
    // the stat isn't set, so charms and consumables can change the dash through StatModifiers.
    /// <summary>How far a dash goes.</summary>
    public float DashDistance => ReadStat(_dashDistanceStat, _settings.DashDistance);
    /// <summary>How long a dash lasts, in seconds.</summary>
    public float DashDuration => ReadStat(_dashDurationStat, _settings.DashDuration);
    /// <summary>How long the invincibility window lasts, in seconds.</summary>
    public float InvincibilityDuration => ReadStat(_dashInvincibilityStat, _settings.InvincibilityDuration);

    /// <summary>Seconds from the start of a dash until the next one: the cooldown stat plus the dash's duration.</summary>
    // INTEGRATION: was a property set once in Awake from _settings. Now worked out each time, so stat changes apply.
    // DashCooldownIcon reads it to fill the icon.
    public float DashCooldown => ReadStat(_dashCooldownStat, _settings.DashCooldown) + DashDuration;

    /// <summary>Fires when a dash starts. PlayerAnimator plays the dash, and DashCooldownIcon starts its countdown.</summary>
    public event Action OnDashed;

    private float _dashTimer;    // Seconds since the current dash started.
    private float _nextDashTime; // Time.time when the next dash is allowed.
    private bool _intendToDash;  // Set by a Dash press, used and cleared by the next HandleDash.

    // This dash's invincibility token. Health stays invincible while any token is held, so this dash only removes its
    // own.
    private object _invincibilityToken;
    private bool _wasInvincible = false; // Last frame's invincibility, so the token is only added or removed on a change.

    // INTEGRATION: new. Source of the dash stats. Set here or by PlayerRoot through SetStats.
    [SerializeField] private Stats _playerStats;

    /// <summary>Hands over the player's Stats, unless one is already assigned.</summary>
    // INTEGRATION: new. PlayerRoot calls this in Awake.
    public void SetStats(Stats playerStats)
    {
        if (_playerStats == null)
        {
            _playerStats = playerStats;
        }
    }

    /// <summary>Checks the orientation is set and finds Health if empty.</summary>
    private void Awake()
    {
        if (!_orientation)
            Debug.LogError($"{nameof(Dash)} requires a {nameof(_orientation)}");

        if (!_health)
            _health = GetComponent<Health>();

        // INTEGRATION: removed setting DashCooldown here. It's now a property that reads the stat.
    }

    /// <summary>Ends a dash in progress, so its invincibility doesn't stay on.</summary>
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

        // INTEGRATION: was _settings.DashDuration + _settings.DashCooldown. DashCooldown now includes the duration.
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
            _dashTimer < _settings.InvincibilityStart + InvincibilityDuration;

        if (shouldBeInvincible == _wasInvincible)
            return;

        if (shouldBeInvincible)
            StartDashInvincibility();
        else
            StopDashInvincibility();

        _wasInvincible = shouldBeInvincible;
    }

    /// <summary>Asks Health for an invincibility token, unless this dash already holds one.</summary>
    private void StartDashInvincibility()
    {
        if (_invincibilityToken != null || !_health || !_settings.InvincibilityEnabled)
            return;

        _invincibilityToken = _health.StartInvincibility();
    }

    /// <summary>Returns this dash's token to Health.</summary>
    private void StopDashInvincibility()
    {
        if (!_health || !_settings.InvincibilityEnabled)
            return;

        _health.StopInvincibility(_invincibilityToken);
        _invincibilityToken = null;
    }

    /// <summary>Requests a dash. The next HandleDash starts it if the cooldown is over.</summary>
    public void IntendToDash() => _intendToDash = true;

    /// <summary>Returns a stat from _playerStats, or the fallback when the stat or Stats isn't set.</summary>
    // INTEGRATION: new. The same lookup as PlayerMovement.ReadStat.
    private float ReadStat(StatDefinition stat, float fallback)
    {
        if (_playerStats == null || stat == null || !_playerStats.HasStat(stat))
            return fallback;

        return _playerStats.GetValue(stat);
    }
}