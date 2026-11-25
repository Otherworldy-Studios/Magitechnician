using System;
using ScriptableSettings;
using MoreMountains.Feedbacks;
using UnityEngine;

/// <summary>
/// The player's dash. PlayerMovement passes Dash presses to IntendToDash and calls HandleDash every frame. A dash
/// moves a set distance over a set time at a constant speed, with an invincibility window partway through.
/// </summary>
public class Dash : MonoBehaviour
{
    private static readonly int FadeId = Shader.PropertyToID("_Fade");

    [SerializeField] private DashSettings _settings;

    [Header("References")]
    // Reference direction used when dashing without movement input, usually camera orientation.
    [SerializeField] private Transform _orientation;
    [SerializeField] private Health _health;
    [SerializeField] private Stats _playerStats;
    [SerializeField] private MMF_Player _dashFeedbacks;

    [Header("Stats")]
    [SerializeField] private StatDefinition _dashDistanceStat;
    [SerializeField] private StatDefinition _dashDurationStat;
    [SerializeField] private StatDefinition _dashCooldownStat;
    [SerializeField] private StatDefinition _dashInvincibilityStat;

    [Header("VFX")]
    [SerializeField] private TrailRenderer[] _dashTrails;
    [SerializeField] private Material _ghostMaterial;
    [SerializeField] private Renderer[] _ghostRenderers;
    [SerializeField] private float _ghostFadeOutTime = 0.1f;

    /// <summary>True during a dash. PlayerMovement uses the dash velocity instead of walking while this is true.</summary>
    public bool IsDashing { get; private set; }

    /// <summary>The direction of the current dash, locked when it starts. PlayerAnimator reads it.</summary>
    public Vector3 DashDirection { get; private set; }

    /// <summary>How far a dash goes.</summary>
    public float DashDistance => ReadStat(_dashDistanceStat, _settings.DashDistance);
    /// <summary>How long a dash lasts, in seconds.</summary>
    public float DashDuration => ReadStat(_dashDurationStat, _settings.DashDuration);
    /// <summary>How long the invincibility window lasts, in seconds.</summary>
    public float InvincibilityDuration => ReadStat(_dashInvincibilityStat, _settings.InvincibilityDuration);

    /// <summary>Seconds from the start of a dash until the next one: the cooldown stat plus the dash's duration.</summary>
    public float DashCooldown => ReadStat(_dashCooldownStat, _settings.DashCooldown) + DashDuration;

    /// <summary>Fires when a dash starts. PlayerAnimator plays the dash, and DashCooldownIcon starts its countdown.</summary>
    public event Action OnDashed;

    private float _dashTimer;    // Seconds since the current dash started.
    private float _nextDashTime; // Time.time when the next dash is allowed.
    private bool _intendToDash;  // Set by a Dash press, used and cleared by the next HandleDash.

    // This dash's invincibility token. Health stays invincible while any token is held, so this dash only removes its
    // own.
    private object _invincibilityToken;
    private bool _wasInvincible; // Last frame's invincibility, so the token is only added or removed on a change.

    private Material[][] _originalMaterials;
    private MaterialPropertyBlock _ghostBlock;
    private bool _ghostActive;
    private float _ghostFadeTimer;

    /// <summary>Hands over the player's Stats, unless one is already assigned.</summary>
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

        _ghostBlock = new MaterialPropertyBlock();
    }

    /// <summary>Ends a dash in progress, so its invincibility doesn't stay on.</summary>
    private void OnDisable()
    {
        CancelDash();
        SetTrailsEmitting(false);
        RestoreMaterials();
    }

    private void LateUpdate()
    {
        SetTrailsEmitting(IsDashing);
        UpdateGhost();
    }

    /// <summary>Returns the dash velocity: the dash distance over its duration, in the dash direction.</summary>
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
                CancelDash();

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

        _nextDashTime = Time.time + DashCooldown;
        _dashFeedbacks.PlayFeedbacks();
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

    public void ClearTrails()
    {
        if (_dashTrails == null)
            return;

        foreach (TrailRenderer trail in _dashTrails)
        {
            if (trail != null)
                trail.Clear();
        }
    }

    private void SetTrailsEmitting(bool emitting)
    {
        if (_dashTrails == null)
            return;

        foreach (TrailRenderer trail in _dashTrails)
        {
            if (trail != null)
                trail.emitting = emitting;
        }
    }

    private void UpdateGhost()
    {
        if (IsDashing)
        {
            if (!_ghostActive)
                ApplyGhost();

            _ghostFadeTimer = _ghostFadeOutTime;
            SetGhostFade(1f);
            return;
        }

        if (!_ghostActive)
            return;

        _ghostFadeTimer -= Time.deltaTime;

        if (_ghostFadeTimer <= 0f)
        {
            RestoreMaterials();
            return;
        }

        SetGhostFade(_ghostFadeTimer / _ghostFadeOutTime);
    }

    private void ApplyGhost()
    {
        if (_ghostMaterial == null || _ghostRenderers == null || _ghostRenderers.Length == 0)
            return;

        _originalMaterials = new Material[_ghostRenderers.Length][];

        for (int i = 0; i < _ghostRenderers.Length; i++)
        {
            Renderer ghostRenderer = _ghostRenderers[i];
            if (ghostRenderer == null)
                continue;

            Material[] original = ghostRenderer.sharedMaterials;
            _originalMaterials[i] = original;

            Material[] ghost = new Material[original.Length];
            for (int j = 0; j < ghost.Length; j++)
            {
                ghost[j] = _ghostMaterial;
            }

            ghostRenderer.sharedMaterials = ghost;
        }

        _ghostActive = true;
    }

    private void SetGhostFade(float fade)
    {
        if (!_ghostActive)
            return;

        _ghostBlock.SetFloat(FadeId, fade);

        foreach (Renderer ghostRenderer in _ghostRenderers)
        {
            if (ghostRenderer != null)
                ghostRenderer.SetPropertyBlock(_ghostBlock);
        }
    }

    private void RestoreMaterials()
    {
        if (!_ghostActive)
            return;

        for (int i = 0; i < _ghostRenderers.Length; i++)
        {
            Renderer ghostRenderer = _ghostRenderers[i];
            if (ghostRenderer == null || _originalMaterials[i] == null)
                continue;

            ghostRenderer.sharedMaterials = _originalMaterials[i];
            ghostRenderer.SetPropertyBlock(null);
        }

        _originalMaterials = null;
        _ghostActive = false;
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
    private float ReadStat(StatDefinition stat, float fallback)
    {
        if (_playerStats == null || stat == null || !_playerStats.HasStat(stat))
            return fallback;

        return _playerStats.GetValue(stat);
    }
}