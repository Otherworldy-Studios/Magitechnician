using System;
using UnityEngine;

/// <summary>The in-game HUD: health, shield, dash cooldown, and crystals.</summary>
public class HUDScreen : UIScreen
{
    [Header("References")]
    [SerializeField] private HealthBar _healthBar;
    [SerializeField] private ShieldBar _shieldBar;
    [SerializeField] private CurrencyDisplay _currencyDisplay;
    [SerializeField] private DashCooldownIcon _dashCooldown;
    [SerializeField] private RoomClearedBanner _roomClearedBanner;

    [Header("Gun Parts")]
    [SerializeField] private ItemSlotUI _barrelSlot;
    [SerializeField] private ItemSlotUI _magazineSlot;
    [SerializeField] private ItemSlotUI _scopeSlot;
    [SerializeField] private ItemSlotUI _stockSlot;
    [SerializeField] private ItemSlotUI _underbarrelSlot;

    private Gun _gun;

    private void Awake()
    {
        if (!_healthBar)
            _healthBar = GetComponentInChildren<HealthBar>();
        if (!_shieldBar)
            _shieldBar = GetComponentInChildren<ShieldBar>();
        if (!_dashCooldown)
            _dashCooldown = GetComponentInChildren<DashCooldownIcon>();
        if (!_currencyDisplay)
            _currencyDisplay = GetComponentInChildren<CurrencyDisplay>();
        if(!_roomClearedBanner)
            _roomClearedBanner = GetComponentInChildren<RoomClearedBanner>();
    }

    private void Start()
    {
        if (!GameManager.HasInstance) return;

        GameManager.Instance.OnPlayerRegistered += BindGun;
        GameManager.Instance.OnRoomClear += EnableRoomClearBanner;

        GameObject player = GameManager.Instance.Player;
        if (player != null) BindGun(player.GetComponent<PlayerRoot>());
        else RefreshParts();
    }

    private void EnableRoomClearBanner()
    {
        _roomClearedBanner.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (GameManager.HasInstance)
            GameManager.Instance.OnPlayerRegistered -= BindGun;

        UnbindGun();
    }

    /// <summary>Shows or hides the health bar, shield bar, and dash icon.</summary>
    public void ToggleHUD(bool enable)
    {
        _healthBar.gameObject.SetActive(enable);
        _shieldBar.gameObject.SetActive(enable);
        _dashCooldown.gameObject.SetActive(enable);
    }

    private void BindGun(PlayerRoot player)
    {
        UnbindGun();

        _gun = player != null ? player.Gun : null;
        if (_gun != null) _gun.OnPartsChanged += RefreshParts;

        RefreshParts();
    }

    private void UnbindGun()
    {
        if (_gun != null) _gun.OnPartsChanged -= RefreshParts;
        _gun = null;
    }

    private void RefreshParts()
    {
        foreach (GunPartType type in Enum.GetValues(typeof(GunPartType)))
        {
            ItemSlotUI slot = GetPartSlot(type);
            if (slot == null) continue;

            slot.SetItem(_gun != null ? _gun.GetEquippedPart(type) : null);
        }
    }

    private ItemSlotUI GetPartSlot(GunPartType type)
    {
        return type switch
        {
            GunPartType.Barrel => _barrelSlot,
            GunPartType.Magazine => _magazineSlot,
            GunPartType.Scope => _scopeSlot,
            GunPartType.Stock => _stockSlot,
            GunPartType.Underbarrel => _underbarrelSlot,
            _ => null
        };
    }
}