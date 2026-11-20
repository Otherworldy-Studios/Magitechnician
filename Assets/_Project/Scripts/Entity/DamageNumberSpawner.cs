using UnityEngine;

/// <summary>
/// Shows floating damage numbers above an enemy whenever its Health takes damage.
/// Put it on the enemy, or anywhere under it: it finds the Health on itself or a parent.
/// </summary>
public class DamageNumberSpawner : MonoBehaviour
{
    [SerializeField] private DamageNumber prefab;

    [Tooltip("Height above the enemy's pivot where numbers appear, in world units.")]
    [SerializeField] private float height = 2f;

    [Tooltip("Hits closer together than this add into one number instead of spawning more. " +
             "This keeps damage over time, which ticks every frame, readable.")]
    [SerializeField] private float mergeWindow = 0.2f;

    private Health _health;
    private float _lastHealth;
    private DamageNumber _current;
    private int _currentSerial;

    private void Awake()
    {
        _health = GetComponentInParent<Health>();

        if (_health == null || prefab == null)
        {
            Debug.LogError($"{name}: DamageNumberSpawner needs a Health on itself or a parent, and a DamageNumber prefab.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (_health == null) return;

        _lastHealth = _health.CurrentHealth;
        _health.OnDamaged += HandleDamaged;
        _health.OnHealed += HandleHealed;
    }

    // Health sets CurrentHealth in its own Awake, which may run after our OnEnable,
    // so read it again once every Awake has finished.
    private void Start()
    {
        if (_health != null)
            _lastHealth = _health.CurrentHealth;
    }

    private void OnDisable()
    {
        if (_health == null) return;

        _health.OnDamaged -= HandleDamaged;
        _health.OnHealed -= HandleHealed;
    }

    // Health.OnDamaged passes the new health rather than the hit,
    // so the damage is how far health dropped since last time.
    private void HandleDamaged(float currentHealth, GameObject source)
    {
        float damage = _lastHealth - currentHealth;
        _lastHealth = currentHealth;
        if (damage <= 0f) return;

        bool canMerge = _current != null
                        && _current.isActiveAndEnabled
                        && _current.Serial == _currentSerial
                        && _current.Age < mergeWindow;

        if (canMerge)
        {
            _current.Add(damage);
            return;
        }

        _current = DamageNumber.Spawn(prefab, _health.transform.position + Vector3.up * height, damage);
        _currentSerial = _current.Serial;
    }

    private void HandleHealed(float currentHealth, GameObject source) => _lastHealth = currentHealth;

    // Shows where numbers will appear when this is selected in the editor.
    private void OnDrawGizmosSelected()
    {
        Health health = GetComponentInParent<Health>();
        if (health == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(health.transform.position + Vector3.up * height, 0.1f);
    }
}