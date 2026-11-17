using UnityEngine;

// Shows a pulsing red vignette and loops a heartbeat while the player's health is below the threshold.
// Only updates when Health.OnDamaged or OnHealed fires, not every frame.
// Lives on the HUD next to HealthBar and connects to each player GameManager registers, the same way HealthBar does.
public class LowHealthWarning : MonoBehaviour
{
    [Header("Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float threshold = 0.25f; // warning turns on below this much health (0.25 = 25%)

    [Header("References")]
    [SerializeField] private Animator vignetteAnimator; // the Animator on FX_FantasyWarrior_Damage_Vignette_01
    [SerializeField] private AudioSource heartbeatSource; // AudioSource with the heartbeat clip

    private static readonly int Active = Animator.StringToHash("Active"); // bool parameter on the vignette's animator

    private Health health;

    void Awake()
    {
        if (heartbeatSource != null)
        {
            heartbeatSource.loop = true;
            heartbeatSource.playOnAwake = false;
        }

        GameManager.Instance.OnPlayerRegistered += HandlePlayerRegistered;
    }

    void Start()
    {
        // If the player registered before this existed, connect to it now
        if (GameManager.Instance.Player != null)
        {
            HandlePlayerRegistered(GameManager.Instance.Player.GetComponent<PlayerRoot>());
        }
        else
        {
            SetWarning(false);
        }
    }

    // The HUD turns off while a menu is open (like pause), and that resets the vignette's animator.
    // When the HUD comes back, this turns the warning back on if health is still low.
    void OnEnable()
    {
        if (vignetteAnimator != null)
        {
            vignetteAnimator.keepAnimatorStateOnDisable = true;
        }

        if (health != null)
        {
            HandleHealthChange(health.CurrentHealth, health.gameObject);
        }
    }

    void OnDestroy()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnPlayerRegistered -= HandlePlayerRegistered;
        }

        Unbind();
    }

    void HandlePlayerRegistered(PlayerRoot player)
    {
        Unbind();

        if (player == null) return;

        health = player.PlayerHealth;
        if (health == null) return;

        health.OnDamaged += HandleHealthChange;
        health.OnHealed += HandleHealthChange;

        // Show the right state for the new player right away
        HandleHealthChange(health.CurrentHealth, health.gameObject);
    }

    void Unbind()
    {
        if (health != null)
        {
            health.OnDamaged -= HandleHealthChange;
            health.OnHealed -= HandleHealthChange;
        }

        health = null;
    }

    // Runs on Health.OnDamaged and OnHealed
    void HandleHealthChange(int currentHealth, GameObject sender)
    {
        if (health == null || health.MaxHealth <= 0) return;

        float percent = (float)currentHealth / health.MaxHealth;

        // At 0 the player is dead, so the death screen shows without the warning
        bool shouldWarn = currentHealth > 0 && percent < threshold;

        SetWarning(shouldWarn);
    }

    void SetWarning(bool on)
    {
        if (vignetteAnimator != null)
        {
            vignetteAnimator.SetBool(Active, on);
        }

        if (heartbeatSource != null)
        {
            if (on && !heartbeatSource.isPlaying)
            {
                heartbeatSource.Play();
            }
            else if (!on && heartbeatSource.isPlaying)
            {
                heartbeatSource.Stop();
            }
        }
    }

    // Right-click the component > Test Warning On / Off while playing
    [ContextMenu("Test Warning On")]
    void TestWarningOn()
    {
        SetWarning(true);
    }

    [ContextMenu("Test Warning Off")]
    void TestWarningOff()
    {
        SetWarning(false);
    }
}