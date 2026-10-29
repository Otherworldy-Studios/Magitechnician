using UnityEngine;
using UnityEngine.UI;

public class HUDScreen : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HealthBar _healthBar;
    [SerializeField] private ShieldBar _shieldBar;

    private void Awake()
    {
        if(!_healthBar)
            _healthBar = GetComponentInChildren<HealthBar>();
        if (!_shieldBar)
            _shieldBar = GetComponentInChildren<ShieldBar>();
    }

    public void ToggleHUD(bool enable)
    {
        _healthBar.gameObject.SetActive(enable);
        _shieldBar.gameObject.SetActive(enable);
    }
}
