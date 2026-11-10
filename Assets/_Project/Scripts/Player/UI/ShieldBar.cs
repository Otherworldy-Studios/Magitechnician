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
        if(!_fill)
            Debug.LogError("No Fill found in ShieldBar");

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


    private void HandlePlayerRegistered(PlayerRoot player)
    {
        Unbind();

        _shieldSystem = player.Shield;
        if (_shieldSystem == null)
            return;

        _shieldSystem.OnShieldDamaged += HandleShieldDamaged;
        _shieldSystem.OnShieldHealed += HandleShieldHealed;
        _shieldSystem.OnShieldBroken += HandleShieldBroken;
        _shieldSystem.OnShieldRestored += HandleShieldRestored;

        // Snap to the new player's health with no smoothing.
        _targetShield = _shieldSystem.CurrentShield / _shieldSystem.MaxShield;
        _visualShield = _targetShield;
        _fill.fillAmount = _visualShield;
        _shieldCounter.text = _shieldSystem.CurrentShield.ToString("F0");
    }

    private void Unbind()
    {
        if (_shieldSystem != null)
        {
            _shieldSystem.OnShieldDamaged -= HandleShieldDamaged;
            _shieldSystem.OnShieldHealed -= HandleShieldHealed;
            _shieldSystem.OnShieldBroken -= HandleShieldBroken;
            _shieldSystem.OnShieldRestored -= HandleShieldRestored;
        }
        _shieldSystem = null;
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
