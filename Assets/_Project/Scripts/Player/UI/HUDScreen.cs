using UnityEngine;

/// <summary>The in-game HUD: health, shield, dash cooldown, and crystals.</summary>
// INTEGRATION: now a UIScreen instead of a plain MonoBehaviour, so UIManager shows and hides it with the other screens.
public class HUDScreen : UIScreen
{
    [Header("References")]
    [SerializeField] private HealthBar _healthBar;
    [SerializeField] private ShieldBar _shieldBar;
    [SerializeField] private CurrencyDisplay _currencyDisplay;    [SerializeField] private DashCooldownIcon _dashCooldown;
    [SerializeField] private Inventory _inventory; // Not read yet.

    /// <summary>Finds the HUD parts in children if empty.</summary>
    private void Awake()
    {
        if (!_healthBar)
            _healthBar = GetComponentInChildren<HealthBar>();
        if (!_shieldBar)
            _shieldBar = GetComponentInChildren<ShieldBar>();
        if (!_dashCooldown)
            _dashCooldown = GetComponentInChildren<DashCooldownIcon>();
        if (!_currencyDisplay)
        {
            _currencyDisplay = GetComponentInChildren<CurrencyDisplay>();
        }
    }

    /// <summary>Shows or hides the health bar, shield bar, and dash icon.</summary>
    public void ToggleHUD(bool enable)
    {
        _healthBar.gameObject.SetActive(enable);
        _shieldBar.gameObject.SetActive(enable);
        _dashCooldown.gameObject.SetActive(enable);
    }
}
