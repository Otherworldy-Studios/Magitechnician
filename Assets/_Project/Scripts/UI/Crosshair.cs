using UnityEngine;

public class CrosshairUI : MonoBehaviour
{
    [Header("Crosshair Arms")]
    [SerializeField] private RectTransform _top;
    [SerializeField] private RectTransform _bottom;
    [SerializeField] private RectTransform _left;
    [SerializeField] private RectTransform _right;

    [Header("Spread")]
    [SerializeField] private float _baseDistance = 15f;
    [SerializeField] private float _spreadMultiplier = 3f;

    [Header("Shot Pulse")]
    [SerializeField] private float _pulseDistance = 10f;
    [SerializeField] private float _recoverySpeed = 60f;

    private float _currentPulse;
    private Gun _gun;

    private void Awake()
    {
        GameManager.Instance.OnPlayerRegistered += HandlePlayerRegistered;
    }

    private void Start()
    {
        if (GameManager.Instance.Player != null)
        {
            HandlePlayerRegistered(
                GameManager.Instance.Player.GetComponent<PlayerRoot>()
            );
        }
    }

    private void Update()
    {
        if (_currentPulse > 0f)
        {
            _currentPulse = Mathf.MoveTowards(
                _currentPulse,
                0f,
                _recoverySpeed * Time.deltaTime
            );

            UpdateSpread();
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

    private void HandlePlayerRegistered(PlayerRoot player)
    {
        Unbind();

        if (player == null) return;

        _gun = player.Gun;

        if (_gun == null) return;

        _gun.OnFired += HandleFired;
        _gun.OnStatsChanged += UpdateSpread;

        UpdateSpread();
    }

    private void Unbind()
    {
        if (_gun != null)
        {
            _gun.OnFired -= HandleFired;
            _gun.OnStatsChanged -= UpdateSpread;
        }

        _gun = null;
        _currentPulse = 0f;
    }

    private void HandleFired()
    {
        _currentPulse = _pulseDistance;
        UpdateSpread();
    }

    private void UpdateSpread()
    {
        if (_gun == null) return;

        float distance = _baseDistance
            + (_gun.Spread * _spreadMultiplier)
            + _currentPulse;

        _top.anchoredPosition = new Vector2(0, distance);
        _bottom.anchoredPosition = new Vector2(0, -distance);
        _left.anchoredPosition = new Vector2(-distance, 0);
        _right.anchoredPosition = new Vector2(distance, 0);
    }
}