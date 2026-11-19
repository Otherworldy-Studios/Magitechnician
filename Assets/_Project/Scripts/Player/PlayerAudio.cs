using UnityEngine;

// Plays the player's sounds through SoundController, each from the event that causes it.
// Put this on the object that has the player's Animator (the character model), so the footstep
// animation events can call Footstep(). Everything else is found through PlayerRoot.
public class PlayerAudio : MonoBehaviour
{
    [Header("Low Health")]
    [Range(0f, 1f)]
    [SerializeField] private float lowHealthThreshold = 0.25f; // heartbeat loops below this much health (0.25 = 25%)

    [Header("Footsteps")]
    [Range(0f, 1f)]
    [SerializeField] private float walkVolume = 0.6f;          // walking steps are quieter
    [Range(0f, 1f)]
    [SerializeField] private float runVolume = 1f;             // full speed steps are louder
    [SerializeField] private float minTimeBetweenSteps = 0.15f; // the blend tree can fire the same step from two clips

    [Header("Landing")]
    [SerializeField] private float minAirTime = 0.3f;  // shorter drops (like walking down a step) make no sound
    [SerializeField] private float loudAirTime = 1f;   // falls this long or longer land at full volume
    [Range(0f, 1f)]
    [SerializeField] private float softLandVolume = 0.4f;

    [Header("Heal")]
    [SerializeField] private float minTimeBetweenHeals = 0.5f; // healing over time heals every frame, so don't spam it

    // Names in SoundController's Sfx Sounds list
    private const string FootstepSound = "Player_Footstep";
    private const string JumpSound = "Player_Jump";
    private const string LandSound = "Player_Land";
    private const string ShieldHitSound = "Player_ShieldHit";
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

    private PlayerRoot root;
    private Health health;
    private ShieldSystem shield;
    private Dash dash;
    private Inventory inventory;
    private PlayerMovement movement;
    private CharacterController controller;
    private PlayerCameraController cameraController;
    private GhostHealth ghostHealth;

    private bool isPaused;
    private bool heartbeatPlaying;
    private float airTime;
    private float lastStepTime = -1f;
    private float lastHealTime = -1f;
    private bool ignoreNextHeal;   // a ghost recovery heals right after its own sound, so skip the heal sound once
    private bool waitingForDash;
    private float dashReadyTime;

    void Awake()
    {
        root = GetComponentInParent<PlayerRoot>();
        if (root == null)
        {
            Debug.LogError("PlayerAudio needs to be under the player's PlayerRoot", this);
            enabled = false;
            return;
        }

        health = root.PlayerHealth;
        shield = root.Shield;
        dash = root.PlayerDash;
        inventory = root.Inventory;
        movement = root.PlayerMovement;
        cameraController = root.GetComponentInChildren<PlayerCameraController>(true);
        ghostHealth = root.GetComponentInChildren<GhostHealth>(true);

        if (movement != null)
        {
            controller = movement.GetComponent<CharacterController>();
        }
    }

    void OnEnable()
    {
        if (root == null) return;

        if (health != null)
        {
            health.OnDamaged += HandleDamaged;
            health.OnHealed += HandleHealed;
            health.OnDied += HandleDied;
        }

        if (shield != null)
        {
            shield.OnShieldDamaged += HandleShieldDamaged;
            shield.OnShieldRegenStarted += HandleShieldRegenStarted;
        }

        if (dash != null) dash.OnDashed += HandleDashed;
        if (movement != null) movement.OnJumped += HandleJumped;
        if (cameraController != null) cameraController.OnAimChanged += HandleAimChanged;
        if (ghostHealth != null) ghostHealth.OnGhostRecovered += HandleGhostRecovered;

        if (inventory != null)
        {
            inventory.OnConsumableUsed += HandleConsumableUsed;
            inventory.OnConsumableEnded += HandleConsumableEnded;
        }

        GameManager.OnPauseToggled += HandlePauseToggled;
    }

    // Unsubscribes from every event, and stops the heartbeat when the player leaves the scene
    void OnDisable()
    {
        if (health != null)
        {
            health.OnDamaged -= HandleDamaged;
            health.OnHealed -= HandleHealed;
            health.OnDied -= HandleDied;
        }

        if (shield != null)
        {
            shield.OnShieldDamaged -= HandleShieldDamaged;
            shield.OnShieldRegenStarted -= HandleShieldRegenStarted;
        }

        if (dash != null) dash.OnDashed -= HandleDashed;
        if (movement != null) movement.OnJumped -= HandleJumped;
        if (cameraController != null) cameraController.OnAimChanged -= HandleAimChanged;
        if (ghostHealth != null) ghostHealth.OnGhostRecovered -= HandleGhostRecovered;

        if (inventory != null)
        {
            inventory.OnConsumableUsed -= HandleConsumableUsed;
            inventory.OnConsumableEnded -= HandleConsumableEnded;
        }

        GameManager.OnPauseToggled -= HandlePauseToggled;

        StopHeartbeat();
    }

    void Update()
    {
        if (isPaused) return;

        CheckLanding();

        // Dash has no "ready" event, so count down its cooldown from the last dash
        if (waitingForDash && Time.time >= dashReadyTime)
        {
            waitingForDash = false;
            Play(DashReadySound);
        }
    }

    #region Footsteps and landing

    // Called by animation events on the walk and run clips, on each foot contact
    public void Footstep()
    {
        if (isPaused || movement == null) return;
        if (!movement.IsGrounded) return;
        if (dash != null && dash.IsDashing) return;
        if (Time.time - lastStepTime < minTimeBetweenSteps) return;

        lastStepTime = Time.time;

        // Louder the faster the player is moving
        float speedPercent = 0f;
        if (controller != null && movement.MaxSpeed > 0f)
        {
            Vector3 velocity = controller.velocity;
            velocity.y = 0f;
            speedPercent = Mathf.Clamp01(velocity.magnitude / movement.MaxSpeed);
        }

        Play(FootstepSound, Mathf.Lerp(walkVolume, runVolume, speedPercent));
    }

    // Counts how long the player has been in the air, and plays the landing sound when they touch down
    void CheckLanding()
    {
        if (movement == null) return;

        if (!movement.IsGrounded)
        {
            airTime += Time.deltaTime;
            return;
        }

        if (airTime >= minAirTime)
        {
            float fallPercent = Mathf.InverseLerp(minAirTime, loudAirTime, airTime);
            Play(LandSound, Mathf.Lerp(softLandVolume, 1f, fallPercent));
        }

        airTime = 0f;
    }

    void HandleJumped()
    {
        Play(JumpSound);
    }

    #endregion

    #region Health and shield

    void HandleDamaged(int currentHealth, GameObject sender)
    {
        UpdateHeartbeat();
    }

    void HandleHealed(int currentHealth, GameObject sender)
    {
        UpdateHeartbeat();

        if (ignoreNextHeal)
        {
            ignoreNextHeal = false;
            return;
        }

        if (Time.time - lastHealTime < minTimeBetweenHeals) return;

        lastHealTime = Time.time;
        Play(HealSound);
    }

    void HandleGhostRecovered(int amount)
    {
        ignoreNextHeal = true;
        Play(GhostRecoverSound);
    }

    void HandleDied(GameObject sender)
    {
        StopHeartbeat();
        Play(DeathSound);
    }

    void HandleShieldDamaged(float currentShield)
    {
        Play(ShieldHitSound);
    }

    void HandleShieldRegenStarted()
    {
        Play(ShieldRechargeSound);
    }

    #endregion

    #region Heartbeat

    // Starts or stops the heartbeat loop based on current health
    void UpdateHeartbeat()
    {
        if (health == null || health.MaxHealth <= 0) return;

        float percent = (float)health.CurrentHealth / health.MaxHealth;
        bool shouldPlay = !isPaused && health.CurrentHealth > 0 && percent < lowHealthThreshold;

        if (shouldPlay && !heartbeatPlaying)
        {
            heartbeatPlaying = true;
            Play(HeartbeatSound);
        }
        else if (!shouldPlay)
        {
            StopHeartbeat();
        }
    }

    void StopHeartbeat()
    {
        if (!heartbeatPlaying) return;

        heartbeatPlaying = false;

        if (SoundController.HasInstance)
        {
            SoundController.Instance.StopSFX(HeartbeatSound);
        }
    }

    void HandlePauseToggled(bool paused)
    {
        isPaused = paused;

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

    void HandleAimChanged(bool aiming)
    {
        Play(aiming ? AimInSound : AimOutSound);
    }

    void HandleDashed()
    {
        waitingForDash = true;
        dashReadyTime = Time.time + dash.DashCooldown;
    }

    void HandleConsumableUsed(Consumable consumable)
    {
        Play(ConsumableUseSound);
    }

    void HandleConsumableEnded(Consumable consumable)
    {
        Play(ConsumableEndSound);
    }

    #endregion

    // Plays a sound from SoundController's Sfx list at the player's position
    void Play(string soundName, float volume = 1f)
    {
        if (!SoundController.HasInstance) return;

        SoundController.Instance.PlaySFX(soundName, transform.position, volume);
    }
}
