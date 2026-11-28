using UnityEngine;
using UnityEngine.UI;
using MoreMountains.Feedbacks;

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
    [SerializeField] private float _pulseHoldTime = 0.06f;
    [SerializeField] private float _recoveryDuration = 0.12f;
    
    [Header("Hit Marker")]
    [SerializeField] private Image _hitMarker;
    [SerializeField] private MMF_Player _hitFeedback;
    [SerializeField] private float _killMarkerMultiplier = 1.4f;

    private Vector3 _hitMarkerBaseScale;

    private float _lastShotTime;
    private float _currentPulse;
    private Gun _gun;

    private void Awake()
    {
        GameManager.Instance.OnPlayerRegistered += HandlePlayerRegistered;
        _hitMarkerBaseScale = _hitMarker.rectTransform.localScale;
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
        if (_currentPulse <= 0f) return;

        float elapsed = Time.time - _lastShotTime;

        if (elapsed < _pulseHoldTime) return;

        float progress = (elapsed - _pulseHoldTime)
                         / Mathf.Max(0.001f, _recoveryDuration);

        _currentPulse = Mathf.SmoothStep(
            _pulseDistance,
            0f,
            Mathf.Clamp01(progress)
        );

        UpdateSpread();
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
        _gun.OnDamageDealt += HandleDamageDealt;

        UpdateSpread();
    }

    private void Unbind()
    {
        if (_gun != null)
        {
            _gun.OnFired -= HandleFired;
            _gun.OnStatsChanged -= UpdateSpread;
            _gun.OnDamageDealt -= HandleDamageDealt;
        }

        _gun = null;
        _currentPulse = 0f;
    }

    private void HandleFired()
    {
        _lastShotTime = Time.time;
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
    
    private void HandleDamageDealt(GameObject target, DamageInfo damageInfo)
    {
        Debug.Log("Crosshair received OnDamageDealt");

        if (target == null || !isActiveAndEnabled) return;

        Debug.Log("Hit target: " + target.name);
        Debug.Log("Hit feedback assigned: " + (_hitFeedback != null));
        Debug.Log("Hit marker assigned: " + (_hitMarker != null));

        EnemyBase enemy = target.GetComponentInParent<EnemyBase>();

        if (enemy == null)
        {
            Debug.Log("EnemyBase not found");
            return;
        }

        Health health = enemy.GetComponentInChildren<Health>(true);

        if (health == null)
        {
            Debug.Log("Health not found");
            return;
        }

        Debug.Log("Enemy health: " + health.CurrentHealth);

        if (_hitFeedback == null || _hitMarker == null) return;

        _hitFeedback.StopFeedbacks();
        _hitFeedback.RestoreInitialValues();

        if (health.CurrentHealth <= 0)
        {
            _hitMarker.color = Color.red;

            _hitMarker.rectTransform.localScale =
                _hitMarkerBaseScale * _killMarkerMultiplier;
        }
        else
        {
            _hitMarker.color = Color.white;

            _hitMarker.rectTransform.localScale =
                _hitMarkerBaseScale;
        }

        Debug.Log("Playing hit marker feedback");

        _hitFeedback.PlayFeedbacks();
    }
}