using UnityEngine;
using UnityEngine.UI;

public class DashCooldownIcon : MonoBehaviour
{
    [SerializeField] private Dash _dash;
    [SerializeField] private Image _icon;
    private bool _isDashing;

    private void Awake()
    {
        GameManager.Instance.OnPlayerRegistered += HandlePlayerRegistered;
    }

    private void Start()
    {
        // If a player registered before this existed, connect to it now.
        if (GameManager.Instance.Player != null)
        {
            HandlePlayerRegistered(GameManager.Instance.Player.GetComponent<PlayerRoot>());
        }
        _icon.fillAmount = 0f;
    }

    private void OnDestroy()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnPlayerRegistered -= HandlePlayerRegistered;
        }
        Unbind();
    }

    private void HandlePlayerRegistered(PlayerRoot player)
    {
        Unbind();

        _dash = player.PlayerDash;
        if (_dash == null)
            return;

        _dash.OnDashed += HandleDashed;
    }

    private void Unbind()
    {
        if (_dash != null)
        {
            _dash.OnDashed -= HandleDashed;
        }
        _dash = null;
    }

    private void Update()
    {
        if (_dash == null || Mathf.Approximately(_icon.fillAmount, 1f)) return;

        _icon.fillAmount =  Mathf.MoveTowards(
            _icon.fillAmount,
            1f,
            Time.deltaTime / _dash.DashCooldown);
    }

    private void HandleDashed()
    {
        _icon.fillAmount = 0f;
    }
}
