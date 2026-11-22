using UnityEngine;

/// <summary>
/// Shows floating damage numbers above an enemy whenever its Health takes damage.
/// Put it on the enemy, or anywhere under it: it finds the Health on itself or a parent.
/// </summary>
public class DamageNumberSpawner : MonoBehaviour
{
    [SerializeField] private DamageNumbers _prefab;

    [Tooltip("Height above the enemy's pivot where numbers appear, in world units.")]
    [SerializeField] private float _height = 2f;

    [Tooltip("Hits closer together than this add into one number instead of spawning more. " +
             "This keeps damage over time, which ticks every frame, readable.")]
    [SerializeField] private float _mergeWindow = 0.2f;

    private Health _health;
    private DamageNumbers _current; // The number the latest hit went into, so quick hits can merge into it.
    private int _currentSerial;     // _current's Serial when it was spawned. A different Serial means it was reused.

    private void Awake()
    {
        _health = GetComponentInParent<Health>();

        if (_health == null || _prefab == null)
        {
            Debug.LogError($"{name}: DamageNumberSpawner needs a Health on itself or a parent, and a DamageNumbers prefab.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (_health == null) return;

        _health.OnDamageInstance += HandleDamageInstance;
    }

    private void OnDisable()
    {
        if (_health == null) return;

        _health.OnDamageInstance -= HandleDamageInstance;
    }

    /// <summary>
    /// Shows the hit's damage. A hit that lands within the merge window of the last one adds to its number instead.
    /// Runs on Health.OnDamageInstance, which passes the whole hit, so overkill shows the full damage.
    /// </summary>
    private void HandleDamageInstance(DamageInfo info)
    {
        int damage = info.DamageAmount;
        if (damage <= 0) return;

        bool canMerge = _current != null
                        && _current.isActiveAndEnabled
                        && _current.Serial == _currentSerial
                        && _current.Age < _mergeWindow;

        if (canMerge)
        {
            _current.Add(damage);
            return;
        }

        _current = DamageNumbers.Spawn(_prefab, _health.transform.position + Vector3.up * _height, damage);
        _currentSerial = _current.Serial;
    }

    private void OnDrawGizmosSelected()
    {
        Health health = GetComponentInParent<Health>();
        if (health == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(health.transform.position + Vector3.up * _height, 0.1f);
    }
}