using UnityEngine;

/// <summary>
/// Shows a pulsing red vignette while the player's health is below the threshold. PlayerAudio plays the heartbeat.
/// Only updates when Health.OnDamaged or OnHealed fires, not every frame.
/// Lives on the HUD next to HealthBar, and connects to each player GameManager registers, the same way HealthBar does.
/// </summary>
public class LowHealthWarning : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The warning turns on below this fraction of max health. 0.25 is 25%.")]
    [SerializeField, Range(0f, 1f)] private float _threshold = 0.25f;

    [Header("References")]
    [Tooltip("The Animator on FX_FantasyWarrior_Damage_Vignette_01.")]
    [SerializeField] private Animator _vignetteAnimator;

    private static readonly int Active = Animator.StringToHash("Active"); // Bool parameter on the vignette's Animator.

    private Health _health;

    private void Awake()
    {
        GameManager.Instance.OnPlayerRegistered += HandlePlayerRegistered;
    }

    private void Start()
    {
        // If the player registered before this existed, connect to it now.
        if (GameManager.Instance.Player != null)
        {
            HandlePlayerRegistered(GameManager.Instance.Player.GetComponent<PlayerRoot>());
        }
        else
        {
            SetWarning(false);
        }
    }

    // The HUD turns off while a menu is open, such as pause, and that resets the vignette's Animator.
    // When the HUD comes back, this turns the warning back on if health is still low.
    private void OnEnable()
    {
        if (_vignetteAnimator != null)
        {
            _vignetteAnimator.keepAnimatorStateOnDisable = true;
        }

        if (_health != null)
        {
            HandleHealthChange(_health.CurrentHealth, _health.gameObject);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnPlayerRegistered -= HandlePlayerRegistered;
        }

        Unbind();
    }

    /// <summary>Moves the health listeners to the new player, and shows the right state for it right away.</summary>
    private void HandlePlayerRegistered(PlayerRoot player)
    {
        Unbind();

        if (player == null) return;

        _health = player.PlayerHealth;
        if (_health == null) return;

        _health.OnDamaged += HandleHealthChange;
        _health.OnHealed += HandleHealthChange;
        _health.OnMaxHealthChanged += HandleHealthChange;

        HandleHealthChange(_health.CurrentHealth, _health.gameObject);
    }

    private void Unbind()
    {
        if (_health != null)
        {
            _health.OnDamaged -= HandleHealthChange;
            _health.OnHealed -= HandleHealthChange;
            _health.OnMaxHealthChanged -= HandleHealthChange;
        }

        _health = null;
    }

    private void HandleHealthChange(int currentHealth, GameObject sender)
    {
        if (_health == null || _health.MaxHealth <= 0) return;

        float percent = (float)currentHealth / _health.MaxHealth;

        // At 0 the player is dead, so the death screen shows without the warning.
        bool shouldWarn = currentHealth > 0 && percent < _threshold;

        SetWarning(shouldWarn);
    }

    private void SetWarning(bool on)
    {
        if (_vignetteAnimator != null)
        {
            _vignetteAnimator.SetBool(Active, on);
        }
    }

    // Right-click the component > Test Warning On or Test Warning Off while playing.
    [ContextMenu("Test Warning On")]
    private void TestWarningOn()
    {
        SetWarning(true);
    }

    [ContextMenu("Test Warning Off")]
    private void TestWarningOff()
    {
        SetWarning(false);
    }
}