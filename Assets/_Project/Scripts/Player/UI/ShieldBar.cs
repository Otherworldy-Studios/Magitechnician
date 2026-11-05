using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShieldBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShieldSystem _shieldSystem;
    [SerializeField] private Image _fill;
    [SerializeField] private TMP_Text _shieldCounter;
    
    [Header("Settings")]
    [SerializeField] private float _smoothDuration = 0.08f;
    
    private float _visualShield = 1f;
    private float _targetShield= 1f;
    private float _velocity;

    private void Awake()
    {
        if(!_shieldSystem)
            Debug.LogError("No ShieldSystem found in ShieldBar");
        if(!_fill)
            Debug.LogError("No Fill found in ShieldBar");
    }

    private void Start()
    {
        _shieldCounter.text = _shieldSystem.CurrentShield.ToString("F0");
    }

    private void OnEnable()
    {
        _shieldSystem.OnShieldDamaged += HandleShieldDamaged;
        _shieldSystem.OnShieldHealed += HandleShieldHealed;
        _shieldSystem.OnShieldBroken += HandleShieldBroken;
        _shieldSystem.OnShieldRestored += HandleShieldRestored;
    }

    private void OnDisable()
    {
        _shieldSystem.OnShieldDamaged -= HandleShieldDamaged;
        _shieldSystem.OnShieldHealed -= HandleShieldHealed;
        _shieldSystem.OnShieldBroken -= HandleShieldBroken;
        _shieldSystem.OnShieldRestored -= HandleShieldRestored;
    }
    
    private void Update()
    {
        if (!Mathf.Approximately(_targetShield, _visualShield))
        {
            _visualShield = Mathf.SmoothDamp(
                _visualShield,
                _targetShield,
                ref _velocity,
                _smoothDuration
            );

            _fill.fillAmount = _visualShield;
        }
    }

    private void HandleShieldDamaged(float damage)
    {
        _targetShield = _shieldSystem.CurrentShield / _shieldSystem.MaxShield;
        _shieldCounter.text = _shieldSystem.CurrentShield.ToString("F0");
    }

    private void HandleShieldHealed()
    {
        _targetShield = _shieldSystem.CurrentShield / _shieldSystem.MaxShield;
        _shieldCounter.text = _shieldSystem.CurrentShield.ToString("F0");
    }

    private void HandleShieldRestored()
    {
        
    }
    
    private void HandleShieldBroken()
    {
        
    }
}
