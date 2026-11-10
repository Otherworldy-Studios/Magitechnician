using UnityEngine;
using UnityEngine.UI;

public class HUDScreen : UIScreen
{
    [Header("References")]
    [SerializeField] private HealthBar _healthBar;
    [SerializeField] private ShieldBar _shieldBar;
    [SerializeField] CurrencyDisplay _currencyDisplay;
    [SerializeField] private DashCooldownIcon _dashCooldown;
    [SerializeField] private Inventory _inventory;
    

    private void Awake()
    {
        if(!_healthBar)
            _healthBar = GetComponentInChildren<HealthBar>();
        if (!_shieldBar)
            _shieldBar = GetComponentInChildren<ShieldBar>();
        if (!_dashCooldown)
            _dashCooldown = GetComponentInChildren<DashCooldownIcon>();
        if(!_currencyDisplay)
        {
            _currencyDisplay = GetComponentInChildren<CurrencyDisplay>();
        }
    }

    public void ToggleHUD(bool enable)
    {
        _healthBar.gameObject.SetActive(enable);
        _shieldBar.gameObject.SetActive(enable);
        _dashCooldown.gameObject.SetActive(enable);
    }
}
