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
        if(!_health)
            Debug.LogError("Health was not assigned in HealthBar");
        /*if(!_ghostFill)
            Debug.LogError("Ghost Fill was not assigned in HealthBar");*/
        if(!_fill)
            Debug.LogError("Fill was not assigned in HealthBar");
    }

    private void Start()
    {
        _healthCounter.text = _health.CurrentHealth.ToString("F0");
    }
    
    private void OnEnable()
    {
        _health.OnDamaged += HandleHealthChange;
        _health.OnHealed += HandleHealthChange;
    }

    private void OnDisable()
    {
        _health.OnDamaged -= HandleHealthChange;
        _health.OnHealed -= HandleHealthChange;
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
