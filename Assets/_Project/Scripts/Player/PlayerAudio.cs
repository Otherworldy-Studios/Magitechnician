using UnityEngine;

/// <summary>
/// Plays the player's sounds through SoundController, each from the event that causes it.
/// Put this on the object that has the player's Animator (the character model), so the footstep animation events can
/// call Footstep. Everything else is found through PlayerRoot.
/// </summary>
public class PlayerAudio : MonoBehaviour
{
    [Header("Low Health")]
    [Tooltip("The heartbeat loops below this fraction of max health. 0.25 is 25%.")]
    [SerializeField, Range(0f, 1f)] private float _lowHealthThreshold = 0.25f;

    [Header("Hurt")]
    [Tooltip("Seconds between hurt sounds, so rapid hits don't stack.")]
    [SerializeField, Min(0f)] private float _minTimeBetweenHurts = 0.2f;

    [Header("Footsteps")]
    [Tooltip("Volume of steps at walking speed. Steps get louder up to Run Volume at full speed.")]
    [SerializeField, Range(0f, 1f)] private float _walkVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float _runVolume = 1f;
    [Tooltip("The blend tree can fire the same step from two clips, so steps closer than this are skipped.")]
    [SerializeField] private float _minTimeBetweenSteps = 0.15f;

    [Header("Landing")]
    [Tooltip("Shorter drops, such as walking down a step, make no sound.")]
    [SerializeField] private float _minAirTime = 0.3f;
    [Tooltip("Falls this long or longer land at full volume.")]
    [SerializeField] private float _loudAirTime = 1f;
    [SerializeField, Range(0f, 1f)] private float _softLandVolume = 0.4f;

    [Header("Heal")]
    [Tooltip("Healing over time heals every frame, so heal sounds closer than this are skipped.")]
    [SerializeField] private float _minTimeBetweenHeals = 0.5f;

    // Names in SoundController's Sfx Sounds list.
    private const string FootstepSound = "Player_Footstep";
    private const string JumpSound = "Player_Jump";
    private const string LandSound = "Player_Land";
    private const string DashSound = "Player_Dash";
    private const string HurtSound = "Player_Hurt";
    private const string ShieldHitSound = "Player_ShieldHit";
    private const string ShieldBreakSound = "Player_ShieldBreak";
    private const string ShieldRechargeSound = "Player_ShieldRecharge";
    private const string HealSound = "Player_Heal";
    private const string GhostRecoverSound = "Player_GhostRecover";
    private const string DeathSound = "Player_Death";
    private const string HeartbeatSound = "Player_Heartbeat";
    private const string AimInSound = "Player_AimIn";
    private const string AimOutSound = "Player_AimOut";
    private const string DashReadySound = "Player_DashReady";
    private const string ConsumableUseSound = "Player_ConsumableUse";
    private const string ConsumableEndSound = "Player_ConsumableEnd";

    private PlayerRoot _root;
    private Health _health;
    private ShieldSystem _shield;
    private Dash _dash;
    private Inventory _inventory;
    private PlayerMovement _movement;
    private CharacterController _controller;
    private PlayerCameraController _cameraController;
    private GhostHealth _ghostHealth;

    private bool _isPaused;
    private bool _heartbeatPlaying;
    private float _lastStepTime = -1f;
    private float _lastHealTime = -1f;
    private float _lastHurtTime = -1f;
    private bool _ignoreNextHeal;   // A ghost recovery heals right after its own sound, so skip the heal sound once.
    private bool _waitingForDash;
    private float _dashReadyTime;

    private void Awake()
    {
        _root = GetComponentInParent<PlayerRoot>();
        if (_root == null)
        {
            Debug.LogError("PlayerAudio needs to be under the player's PlayerRoot", this);
            enabled = false;
            return;
        }

        _health = _root.PlayerHealth;
        _shield = _root.Shield;
        _dash = _root.PlayerDash;
        _inventory = _root.Inventory;
        _movement = _root.PlayerMovement;
        _cameraController = _root.GetComponentInChildren<PlayerCameraController>(true);
        _ghostHealth = _root.GetComponentInChildren<GhostHealth>(true);

        if (_movement != null)
        {
            _controller = _movement.GetComponent<CharacterController>();
        }
    }

    private void OnEnable()
    {
        if (_root == null) return;

        if (_health != null)
        {
            _health.OnDamaged += HandleDamaged;
            _health.OnHealed += HandleHealed;
            _health.OnDied += HandleDied;
        }

        if (_shield != null)
        {
            _shield.OnShieldDamaged += HandleShieldDamaged;
            _shield.OnShieldBroken += HandleShieldBroken;
            _shield.OnShieldRegenStarted += HandleShieldRegenStarted;
        }

        if (_movement != null)
        {
            _movement.OnJumped += HandleJumped;
            _movement.OnLanded += HandleLanded;
        }

        if (_dash != null) _dash.OnDashed += HandleDashed;
        if (_cameraController != null) _cameraController.OnAimChanged += HandleAimChanged;
        if (_ghostHealth != null) _ghostHealth.OnGhostRecovered += HandleGhostRecovered;

        if (_inventory != null)
        {
            _inventory.OnConsumableUsed += HandleConsumableUsed;
            _inventory.OnConsumableEnded += HandleConsumableEnded;
        }

        GameManager.OnPauseToggled += HandlePauseToggled;
    }

    // Also stops the heartbeat when the player leaves the scene.
    private void OnDisable()
    {
        if (_health != null)
        {
            _health.OnDamaged -= HandleDamaged;
            _health.OnHealed -= HandleHealed;
            _health.OnDied -= HandleDied;
        }

        if (_shield != null)
        {
            _shield.OnShieldDamaged -= HandleShieldDamaged;
            _shield.OnShieldBroken -= HandleShieldBroken;
            _shield.OnShieldRegenStarted -= HandleShieldRegenStarted;
        }

        if (_movement != null)
        {
            _movement.OnJumped -= HandleJumped;
            _movement.OnLanded -= HandleLanded;
        }

        if (_dash != null) _dash.OnDashed -= HandleDashed;
        if (_cameraController != null) _cameraController.OnAimChanged -= HandleAimChanged;
        if (_ghostHealth != null) _ghostHealth.OnGhostRecovered -= HandleGhostRecovered;

        if (_inventory != null)
        {
            _inventory.OnConsumableUsed -= HandleConsumableUsed;
            _inventory.OnConsumableEnded -= HandleConsumableEnded;
        }

        GameManager.OnPauseToggled -= HandlePauseToggled;

        StopHeartbeat();
    }

    private void Update()
    {
        if (_isPaused) return;

        // Dash has no "ready" event, so count down its cooldown from the last dash.
        if (_waitingForDash && Time.time >= _dashReadyTime)
        {
            _waitingForDash = false;
            Play(DashReadySound);
        }
    }

    #region Footsteps, jumping and landing

    /// <summary>
    /// Plays a footstep, louder the faster the player moves. Called by animation events on the walk and run clips,
    /// on each foot contact.
    /// </summary>
    public void Footstep()
    {
        if (_isPaused || _movement == null) return;
        if (!_movement.IsGrounded) return;
        if (_dash != null && _dash.IsDashing) return;
        if (Time.time - _lastStepTime < _minTimeBetweenSteps) return;

        _lastStepTime = Time.time;

        float speedPercent = 0f;
        if (_controller != null && _movement.MaxSpeed > 0f)
        {
            Vector3 velocity = _controller.velocity;
            velocity.y = 0f;
            speedPercent = Mathf.Clamp01(velocity.magnitude / _movement.MaxSpeed);
        }

        Play(FootstepSound, Mathf.Lerp(_walkVolume, _runVolume, speedPercent));
    }

    private void HandleJumped()
    {
        Play(JumpSound);
    }

    /// <summary>Plays the landing sound, louder for longer falls. Drops shorter than _minAirTime make no sound.</summary>
    private void HandleLanded(float airTime)
    {
        if (_isPaused || airTime < _minAirTime) return;

        float fallPercent = Mathf.InverseLerp(_minAirTime, _loudAirTime, airTime);
        Play(LandSound, Mathf.Lerp(_softLandVolume, 1f, fallPercent));
    }

    #endregion

    #region Health and shield

    /// <summary>Plays the hurt sound, unless the hit killed the player (HandleDied plays the death sound instead).</summary>
    private void HandleDamaged(int currentHealth, GameObject sender)
    {
        UpdateHeartbeat();

        if (currentHealth <= 0) return;
        if (Time.time - _lastHurtTime < _minTimeBetweenHurts) return;

        _lastHurtTime = Time.time;
        Play(HurtSound);
    }

    private void HandleHealed(int currentHealth, GameObject sender)
    {
        UpdateHeartbeat();

        if (_ignoreNextHeal)
        {
            _ignoreNextHeal = false;
            return;
        }

        if (Time.time - _lastHealTime < _minTimeBetweenHeals) return;

        _lastHealTime = Time.time;
        Play(HealSound);
    }

    private void HandleGhostRecovered(int amount)
    {
        _ignoreNextHeal = true;
        Play(GhostRecoverSound);
    }

    private void HandleDied(GameObject sender)
    {
        StopHeartbeat();
        Play(DeathSound);
    }

    /// <summary>
    /// Plays the shield hit sound. A hit that breaks the shield leaves it at 0, and HandleShieldBroken plays the
    /// break sound for that hit instead.
    /// </summary>
    private void HandleShieldDamaged(float currentShield)
    {
        if (currentShield <= 0f) return;

        Play(ShieldHitSound);
    }

    private void HandleShieldBroken()
    {
        Play(ShieldBreakSound);
    }

    private void HandleShieldRegenStarted()
    {
        Play(ShieldRechargeSound);
    }

    #endregion

    #region Heartbeat

    /// <summary>Starts or stops the heartbeat loop based on current health.</summary>
    private void UpdateHeartbeat()
    {
        if (_health == null || _health.MaxHealth <= 0) return;

        float percent = (float)_health.CurrentHealth / _health.MaxHealth;
        bool shouldPlay = !_isPaused && _health.CurrentHealth > 0 && percent < _lowHealthThreshold;

        if (shouldPlay && !_heartbeatPlaying)
        {
            _heartbeatPlaying = true;
            Play(HeartbeatSound);
        }
        else if (!shouldPlay)
        {
            StopHeartbeat();
        }
    }

    private void StopHeartbeat()
    {
        if (!_heartbeatPlaying) return;

        _heartbeatPlaying = false;

        if (SoundController.HasInstance)
        {
            SoundController.Instance.StopSFX(HeartbeatSound);
        }
    }

    private void HandlePauseToggled(bool paused)
    {
        _isPaused = paused;

        if (paused)
        {
            StopHeartbeat();
        }
        else
        {
            UpdateHeartbeat();
        }
    }

    #endregion

    #region Aim, dash, and consumables

    private void HandleAimChanged(bool aiming)
    {
        Play(aiming ? AimInSound : AimOutSound);
    }

    /// <summary>Plays the dash sound, and starts counting down to the dash-ready sound.</summary>
    private void HandleDashed()
    {
        Play(DashSound);

        _waitingForDash = true;
        _dashReadyTime = Time.time + _dash.DashCooldown;
    }

    private void HandleConsumableUsed(Consumable consumable)
    {
        Play(ConsumableUseSound);
    }

    private void HandleConsumableEnded(Consumable consumable)
    {
        Play(ConsumableEndSound);
    }

    #endregion

    /// <summary>Plays a sound from SoundController's Sfx list at the player's position.</summary>
    private void Play(string soundName, float volume = 1f)
    {
        if (!SoundController.HasInstance) return;

        SoundController.Instance.PlaySFX(soundName, transform.position, volume);
    }
}