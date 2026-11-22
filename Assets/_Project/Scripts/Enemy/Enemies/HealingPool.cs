using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The pool the Skeletal Orc's cannonball leaves where it lands. Every Tick Interval, damages the player and heals
/// every living enemy inside its radius, then removes itself after its lifetime. ArcProjectileAttack spawns it and
/// passes the orc as the damage source.
/// </summary>
public class HealingPool : MonoBehaviour
{
    [Tooltip("Meters from the pool's center that count as inside.")]
    [SerializeField, Min(0f)] private float _radius = 3f;
    [Tooltip("Seconds before the pool disappears.")]
    [SerializeField, Min(0f)] private float _lifetime = 6f;
    [Tooltip("Seconds between each round of damage and healing. The first round is one interval after the pool appears.")]
    [SerializeField, Min(0.05f)] private float _tickInterval = 1f;
    [Tooltip("Damage dealt to the player each tick.")]
    [SerializeField, Min(0)] private int _damagePerTick = 5;
    [Tooltip("Health restored to each enemy each tick.")]
    [SerializeField, Min(0)] private int _healPerTick = 5;
    [Tooltip("Layers checked each tick. Set to Player and Enemy.")]
    [SerializeField] private LayerMask _affectedLayers = ~0;

    private readonly HashSet<Object> _handledThisTick = new(); // So a target with several colliders is affected once.
    private GameObject _source; // The orc, used as the DamageSource. Can be gone once the orc dies.
    private float _nextTickTime;

    /// <summary>Sets the object that made the pool. ArcProjectileAttack calls this right after spawning it.</summary>
    public void Initialize(GameObject source)
    {
        _source = source;
    }

    private void Start()
    {
        _nextTickTime = Time.time + _tickInterval;
        Destroy(gameObject, _lifetime);
    }

    private void Update()
    {
        if (Time.time < _nextTickTime)
            return;

        _nextTickTime += _tickInterval;
        Tick();
    }

    /// <summary>Damages the player and heals each enemy inside the radius once.</summary>
    private void Tick()
    {
        _handledThisTick.Clear();

        Collider[] hits = Physics.OverlapSphere(transform.position, _radius, _affectedLayers, QueryTriggerInteraction.Collide);

        foreach (Collider hit in hits)
        {
            EnemyBase enemy = hit.GetComponentInParent<EnemyBase>();
            if (enemy != null)
            {
                if (!enemy.IsDead && _handledThisTick.Add(enemy))
                    enemy.Health.RegenHealth(_healPerTick);

                continue;
            }

            PlayerRoot player = hit.GetComponentInParent<PlayerRoot>();
            if (player == null || !_handledThisTick.Add(player))
                continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null)
                continue;

            damageable.TakeDamage(new DamageInfo
            {
                DamageAmount = _damagePerTick,
                DamageSource = _source,
                DamageDirection = Vector3.zero,
                DamageType = DamageType.Physical
            });
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
