using System;
using UnityEngine;

/// <summary>
/// A bullet. Moves forward each frame with a sphere cast so it can't skip through thin objects, damages the first
/// IDamageable it hits, and destroys itself on a hit or once it has traveled its range.
/// Whoever fires it (the Gun) passes the damage, speed, and range in Initialize.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Tooltip("The bullet's radius and the layers it can hit.")]
    [SerializeField] private ProjectileSettings _settings;

    private Vector3 _direction; // Set from the spawn rotation in Start. The Gun's spread is already in that rotation.
    private float _travelTime;  // Seconds since the bullet started, for the range check.

    // INTEGRATION: new. Initialize fills these from the Gun's stats at the moment of firing, replacing
    // ProjectileSettings' Speed, MaxDistance, and Damage, which were removed.
    private DamageInfo _damageInfo;      // The hit to deal. Its DamageSource is the player, so the bullet skips the player.
    private float _speed;                // Meters per second, from Stat_BulletSpeed.
    private float _range;                // Meters before the bullet is destroyed, from Stat_Range.
    private Action<DamageInfo> _onHit;   // Called after a hit. The Gun uses it to fire OnDamageDealt.
    private bool _initialized;           // False until Initialize runs. Start destroys an uninitialized bullet.

    /// <summary>The bullet's radius. CrossHairAim uses it so the aim check matches the bullet's size.</summary>
    // INTEGRATION: new. Gun.BulletRadius reads it from the prefab, and Shoot passes it to CrossHairAim.Solve.
    public float Radius => _settings != null ? _settings.Radius : 0f;

    /// <summary>Sets what this bullet deals and how far and fast it travels. Call right after creating it.</summary>
    /// <param name="damageInfo">The hit, with the shooter as DamageSource.</param>
    /// <param name="speed">Meters per second.</param>
    /// <param name="range">Meters before it's destroyed.</param>
    /// <param name="onHit">Called after the bullet damages something. Optional.</param>
    // INTEGRATION: replaces the commented-out Initialize(GameObject attacker). Gun.TryFire calls it.
    public void Initialize(DamageInfo damageInfo, float speed, float range, Action<DamageInfo> onHit = null)
    {
        _damageInfo = damageInfo;
        _speed = speed;
        _range = range;
        _onHit = onHit;
        _initialized = true;
    }

    /// <summary>Destroys a bullet that wasn't initialized, sets the direction, and sets a 5 second limit as a backup.</summary>
    private void Start()
    {
        // INTEGRATION: this check was commented out. It's on again because Initialize works.
        if (!_initialized)
        {
            Debug.LogError("Projectile was not initialized!");
            Destroy(gameObject);
            return;
        }

        _direction = transform.forward.normalized;
        Destroy(gameObject, 5f); // Backup in case the range check never triggers.
    }

    /// <summary>
    /// Moves the bullet forward and checks for a hit along the way with a sphere cast, so it can't miss objects
    /// between frames. Skips the shooter's colliders. Destroys the bullet on a hit or past its range.
    /// </summary>
    private void Update()
    {
        _travelTime += Time.deltaTime;
        if (_travelTime * _speed > _range) // INTEGRATION: uses the speed and range from Initialize.
        {
            Destroy(gameObject);
            return;
        }

        Vector3 oldPos = transform.position;
        float step = _speed * Time.deltaTime;
        Vector3 newPos = oldPos + _direction * step;

        // Start the cast one radius behind, so the bullet still catches something it's already touching.
        Vector3 castOrigin = oldPos - _direction * _settings.Radius;
        float castDist = step + _settings.Radius;

        if (Physics.SphereCast(
                castOrigin,
                _settings.Radius,
                _direction,
                out RaycastHit hit,
                castDist,
                _settings.BulletCollisionLayer,
                QueryTriggerInteraction.Collide))
        {
            // INTEGRATION: the owner check was commented out. A hit on the shooter is skipped and the bullet keeps going.
            if (_damageInfo.DamageSource != null && hit.collider.transform.IsChildOf(_damageInfo.DamageSource.transform))
            {
                transform.position = newPos;
                return;
            }

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

            // INTEGRATION: the damage code was commented out, so bullets didn't deal damage.
            if (damageable != null)
            {
                damageable.TakeDamage(_damageInfo);
                _onHit?.Invoke(_damageInfo); // INTEGRATION: lets the Gun fire OnDamageDealt.
            }

            Destroy(gameObject);
            return;
        }

        transform.position = newPos; // Nothing hit, so move forward.
    }
}
