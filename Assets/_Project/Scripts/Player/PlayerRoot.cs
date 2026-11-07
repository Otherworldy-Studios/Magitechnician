using UnityEngine;

public class PlayerRoot : MonoBehaviour
{
    [SerializeField] private Health _health;
    [SerializeField] private ShieldSystem shield;
    [SerializeField] private Dash dash;
    public Health PlayerHealth => _health;
    public ShieldSystem Shield => shield;
    public Dash PlayerDash => dash;

    private void Awake()
    {
        if (!_health)
        {
            _health = GetComponentInChildren<Health>();
        }
    }

    private void Start()
    {
        GameManager.Instance.RegisterPlayer(this);
    }
}

//TODO: also give reference to shield, stats, inventory, gun