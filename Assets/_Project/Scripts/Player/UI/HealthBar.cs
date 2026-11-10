using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health _health;
    [SerializeField] private Image _fill;
    [SerializeField] private Image _ghostFill;
    [SerializeField] private TMP_Text _healthCounter;
    
    [Header("Settings")]
    [SerializeField] private float _smoothDuration = 0.08f;
    
    private float _visualHealth = 1f;
    private float _targetHealth = 1f;
    private float _velocity;

    private void Awake()
    {
        if (!_fill)
            Debug.LogError("Fill was not assigned in HealthBar");

        GameManager.Instance.OnPlayerRegistered += HandlePlayerRegistered;
    }

    private void Start()
    {
        // If a player registered before this bar existed, connect to it now.
        if (GameManager.Instance.Player != null)
        {
            HandlePlayerRegistered(GameManager.Instance.Player.GetComponent<PlayerRoot>());
        }
        _healthCounter.text = _health.CurrentHealth.ToString("F0");
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

        _health = player.PlayerHealth;
        if (_health == null)
            return;

        _health.OnDamaged += HandleHealthChange;
        _health.OnHealed += HandleHealthChange;

        // Snap to the new player's health with no smoothing.
        _targetHealth = (float)_health.CurrentHealth / _health.MaxHealth;
        _visualHealth = _targetHealth;
        _fill.fillAmount = _visualHealth;
        _healthCounter.text = _health.CurrentHealth.ToString("F0");
    }

    private void Unbind()
    {
        if (_health != null)
        {
            _health.OnDamaged -= HandleHealthChange;
            _health.OnHealed -= HandleHealthChange;
        }
        _health = null;
    }


   
    
    private void Update()
    {
        if (!Mathf.Approximately(_targetHealth, _visualHealth))
        {
            _visualHealth = Mathf.SmoothDamp(
                _visualHealth,
                _targetHealth,
                ref _velocity,
                _smoothDuration
            );

            _fill.fillAmount = _visualHealth;
        }
    }

    private void HandleHealthChange(int amount, GameObject sender)
    {
        _targetHealth = (float)_health.CurrentHealth / _health.MaxHealth;
        _healthCounter.text = _health.CurrentHealth.ToString("F0");
    }

    private void HandleGhostHealthChange(float ghostHealth)
    {
        
    }
}
