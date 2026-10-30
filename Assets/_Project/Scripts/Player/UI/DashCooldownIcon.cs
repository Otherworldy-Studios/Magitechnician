using UnityEngine;
using UnityEngine.UI;

public class DashCooldownIcon : MonoBehaviour
{
    [SerializeField] private Dash _dash;
    [SerializeField] private Image _icon;
    private bool _isDashing;

    private void Awake()
    {
        if(!_dash)
            Debug.LogError("DashCooldownIcon needs a Dash component");
    }

    private void Start()
    {
        _icon.fillAmount = 0f;
    }

    void OnEnable()
    {
        _dash.OnDashed += HandleDashed;
    }

    void OnDisable()
    {
        _dash.OnDashed -= HandleDashed;
    }

    private void Update()
    {
        if (Mathf.Approximately(_icon.fillAmount, 1f))
            return;
        
        _icon.fillAmount =  Mathf.MoveTowards(
            _icon.fillAmount,
            1f,
            Time.deltaTime / _dash.DashCooldown);
    }

    private void HandleDashed()
    {
        _icon.fillAmount = 0f;
    }
}
