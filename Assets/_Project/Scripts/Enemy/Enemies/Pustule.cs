using System;
using UnityEngine;

/// <summary>
/// The Corrupted Insectoid's weak point. Goes on a child object with its own collider, DamageReceiver, and a Health
/// with Max Health 1, so bullets that hit it find its DamageReceiver before the host's. When its Health runs out, it
/// explodes, damaging everything nearby once, then kills the host. EnemyBase.Die only runs once, so the host still
/// counts as one kill.
/// </summary>
[RequireComponent(typeof(Health))]
public class Pustule : MonoBehaviour
{
    [Tooltip("The insectoid this pustule belongs to. Found in parents if empty.")]
    [SerializeField] private EnemyBase _host;

    [Header("Explosion")]
    [Tooltip("Meters from the pustule that the explosion reaches.")]
    [SerializeField, Min(0f)] private float _explosionRadius = 3f;
    [Tooltip("Damage dealt once to each player or enemy in the radius.")]
    [SerializeField, Min(0)] private int _explosionDamage = 30;
    [Tooltip("Layers the explosion can hit. Set to Player and Enemy.")]
    [SerializeField] private LayerMask _explosionLayers = ~0;
    [Tooltip("Spawned at the pustule when it explodes. Optional.")]
    [SerializeField] private GameObject _explosionEffectPrefab;
    [Tooltip("Turned off when the pustule explodes, such as its mesh. Optional.")]
    [SerializeField] private GameObject _visual;

    private Health _health;
    private bool _exploded;

    /// <summary>Fires once when the pustule explodes, with the explosion's center. For the explosion sound.</summary>
    public event Action<Vector3> OnExploded;

    private void Awake()
    {
        _health = GetComponent<Health>();

        if (_host == null)
            _host = GetComponentInParent<EnemyBase>();
    }

    private void OnEnable()
    {
        _health.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        _health.OnDied -= HandleDied;
    }

    private void HandleDied(GameObject pustule)
    {
        Explode();
    }

    /// <summary>Damages everything in the radius except the host, plays the effect, then kills the host. Runs once.</summary>
    private void Explode()
    {
        if (_exploded)
            return;

        _exploded = true;
        Vector3 center = transform.position;

        DamageInfo damageInfo = new DamageInfo
        {
            DamageAmount = _explosionDamage,
            DamageSource = _host != null ? _host.gameObject : gameObject,
            DamageDirection = Vector3.zero,
            DamageType = DamageType.Physical
        };

        Explosion.Explode(center, _explosionRadius, damageInfo, _explosionLayers, _host != null ? _host.transform : transform);

        if (_explosionEffectPrefab != null)
            Instantiate(_explosionEffectPrefab, center, Quaternion.identity);

        if (_visual != null)
            _visual.SetActive(false);

        OnExploded?.Invoke(center);

        if (_host != null)
            _host.Die();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}
