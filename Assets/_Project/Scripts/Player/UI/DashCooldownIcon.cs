using UnityEngine;
using UnityEngine.UI;

/// <summary>Fills the dash icon over the dash cooldown. Empties when a dash starts and is full when the dash is ready.</summary>
// INTEGRATION: no longer needs a reference set in the Inspector. The HUD lives in the persistent UI, while the player
// is spawned per scene, so this connects to each player GameManager registers (GameManager.OnPlayerRegistered, new).
public class DashCooldownIcon : MonoBehaviour
{
    [SerializeField] private Dash _dash;
    [SerializeField] private Image _icon;
    private bool _isDashing; // Not read yet.

    /// <summary>Starts listening for player registration.</summary>
    // INTEGRATION: was a check that _dash was assigned.
    private void Awake()
    {
        GameManager.Instance.OnPlayerRegistered += HandlePlayerRegistered;
    }

    /// <summary>Connects to a player that registered before this existed, and starts the icon empty.</summary>
    private void Start()
    {
        // If a player registered before this existed, connect to it now.
        if (GameManager.Instance.Player != null)
        {
            HandlePlayerRegistered(GameManager.Instance.Player.GetComponent<PlayerRoot>());
        }
        _icon.fillAmount = 0f;
    }

    /// <summary>Stops listening to GameManager and the player.</summary>
    private void OnDestroy()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnPlayerRegistered -= HandlePlayerRegistered;
        }
        Unbind();
    }

    /// <summary>
    /// Connects to the newly registered player's Dash. Runs on GameManager.OnPlayerRegistered, and from Start when a player
    /// was already registered.
    /// </summary>
    // INTEGRATION: replaces subscribing to OnDashed in OnEnable.
    private void HandlePlayerRegistered(PlayerRoot player)
    {
        Unbind();

        _dash = player.PlayerDash;
        if (_dash == null)
            return;

        _dash.OnDashed += HandleDashed;
    }

    /// <summary>Disconnects from the current player, if any.</summary>
    private void Unbind()
    {
        if (_dash != null)
        {
            _dash.OnDashed -= HandleDashed;
        }
        _dash = null;
    }

    /// <summary>Fills the icon toward full at a rate that takes DashCooldown seconds.</summary>
    private void Update()
    {
        if (_dash == null || Mathf.Approximately(_icon.fillAmount, 1f)) return;

        // DashCooldown includes the dash's duration and reads the stat, so the fill matches dash items.
        _icon.fillAmount = Mathf.MoveTowards(
            _icon.fillAmount,
            1f,
            Time.deltaTime / _dash.DashCooldown);
    }

    /// <summary>Empties the icon when a dash starts. Runs on Dash.OnDashed.</summary>
    private void HandleDashed()
    {
        _icon.fillAmount = 0f;
    }
}
