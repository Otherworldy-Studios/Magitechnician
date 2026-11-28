using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MoreMountains.Feedbacks;

/// <summary>Shows the player's shield as a fill and a number. The fill eases toward the new value.</summary>
// Connects to each player GameManager registers, because the HUD lives in the persistent UI while each scene has its
// own player.
public class ShieldBar : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Set by HandlePlayerRegistered. Don't set it by hand.")]
    [SerializeField] private ShieldSystem _shieldSystem;
    [SerializeField] private Image _fill;
    [SerializeField] private TMP_Text _shieldCounter;
    [SerializeField] private MMF_Player _shieldBreakFeedback;

    [Header("Settings")]
    [Tooltip("Seconds the fill takes to catch up.")]
    [SerializeField] private float _smoothDuration = 0.08f;

    private float _visualShield = 1f; // The fill shown now, from 0 to 1.
    private float _targetShield = 1f; // The fill to ease toward: current shield over max shield.
    private float _velocity;          // Used by SmoothDamp between frames.

    private void Awake()
    {
        if (!_fill)
            Debug.LogError("No Fill found in ShieldBar", this);

        GameManager.Instance.OnPlayerRegistered += HandlePlayerRegistered;
    }

    private void Start()
    {
        // If a player registered before this bar existed, connect to it now.
        if (GameManager.Instance.Player != null)
        {
            HandlePlayerRegistered(GameManager.Instance.Player.GetComponent<PlayerRoot>());
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

    /// <summary>
    /// Connects to the newly registered player's ShieldSystem and shows its value at once. Runs on
    /// GameManager.OnPlayerRegistered, and from Start when a player was already registered.
    /// </summary>
    private void HandlePlayerRegistered(PlayerRoot player)
    {
        Unbind();

        if (player == null) return;

        _shieldSystem = player.Shield;
        if (_shieldSystem == null) return;

        _shieldSystem.OnShieldDamaged += HandleShieldDamaged;
        _shieldSystem.OnShieldHealed += HandleShieldHealed;
        _shieldSystem.OnMaxShieldChanged += Refresh;
        _shieldSystem.OnShieldBroken += HandleShieldBroken;

        // Snap to the new player's shield with no smoothing.
        Refresh();
        _visualShield = _targetShield;
        _fill.fillAmount = _visualShield;
    }

    /// <summary>Disconnects from the current player, if any.</summary>
    private void Unbind()
    {
        if (_shieldSystem != null)
        {
            _shieldSystem.OnShieldDamaged -= HandleShieldDamaged;
            _shieldSystem.OnShieldHealed -= HandleShieldHealed;
            _shieldSystem.OnMaxShieldChanged -= Refresh;
            _shieldSystem.OnShieldBroken -= HandleShieldBroken;
        }

        _shieldSystem = null;
    }

    private void Update()
    {
        if (!Mathf.Approximately(_targetShield, _visualShield))
        {
            _visualShield = Mathf.SmoothDamp(_visualShield, _targetShield, ref _velocity, _smoothDuration);
            _fill.fillAmount = _visualShield;
        }
    }

    private void HandleShieldDamaged(float currentShield)
    {
        Refresh();
    }
    
    private void HandleShieldHealed()
    {
        Refresh();

        if (_shieldBreakFeedback != null)
        {
            _shieldBreakFeedback.StopFeedbacks();
            _shieldBreakFeedback.RestoreInitialValues();
        }
    }

    /// <summary>Sets the new target and number. Runs on OnShieldDamaged, OnShieldHealed, and OnMaxShieldChanged.</summary>
    private void Refresh()
    {
        if (_shieldSystem == null) return;

        float maxShield = _shieldSystem.MaxShield;
        _targetShield = maxShield > 0f ? _shieldSystem.CurrentShield / maxShield : 0f;
        _shieldCounter.text = _shieldSystem.CurrentShield.ToString("F0");
    }
    
    private void HandleShieldBroken()
    {
        if (_shieldBreakFeedback != null)
        {
            _shieldBreakFeedback.PlayFeedbacks();
        }
    }
}