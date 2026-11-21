using System;
using UnityEngine;

/// <summary>
/// A bullet. Moves each frame with a sphere cast so it can't skip through thin objects, damages the first
/// IDamageable it hits, and destroys itself on a hit or once it has traveled its range. Can fall with gravity.
/// Whoever fires it (the Gun or an enemy attack) passes the damage, speed, and range in Initialize.
/// A collider with an IProjectileBlocker on it, such as the Dark Knight's shield, stops the projectile without damage.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Tooltip("The bullet's radius and the layers it can hit.")]
    [SerializeField] private ProjectileSettings _settings;

    [Header("Impacts")]
    [Tooltip("Spawned where the projectile hits a wall, the floor, a shield, or anything else that can't take damage. Optional.")]
    [SerializeField] private GameObject _surfaceImpactPrefab;

    [Tooltip("Spawned where the projectile hits something that takes damage, such as an enemy. Optional.")]
    [SerializeField] private GameObject _damageableImpactPrefab;

    private DamageInfo _damageInfo;      
    private float _speed;               
    private float _range;             
    private bool _useGravity;            // True for arcing projectiles, such as the Skeletal Orc's cannonball.
    private Action<GameObject, DamageInfo> _onHit; 
    private bool _initialized;        

    private Vector3 _direction; // Set from the spawn rotation in Start. The Gun's spread is already in that rotation.
    private Vector3 _velocity;  // Used only with gravity.
    private float _travelTime;  // Seconds since the bullet started, for the range check.

    /// <summary>The bullet's radius. CrossHairAim uses it so the aim check matches the bullet's size.</summary>
    public float Radius => _settings != null ? _settings.Radius : 0f;

    
    public event Action<Vector3, Vector3> OnImpact;

    public void Initialize(DamageInfo damageInfo, float speed, float range, Action<GameObject, DamageInfo> onHit = null, bool useGravity = false)
    {
        _damageInfo = damageInfo;
        _speed = speed;
        _range = range;
        _onHit = onHit;
        _useGravity = useGravity;
        _initialized = true;
    }

    private void Start()
    {
        if (!_initialized)
        {
            Debug.LogError("Projectile was not initialized!");
            Destroy(gameObject);
            return;
        }

        _direction = transform.forward.normalized;
        _velocity = _direction * _speed;

        Destroy(gameObject, 5f); // Backup in case the range check never triggers.
    }

    private void Update()
    {
        _travelTime += Time.deltaTime;
        if (_travelTime * _speed > _range)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 oldPos = transform.position;
        Vector3 displacement;

        if (_useGravity)
        {
            displacement = _velocity * Time.deltaTime + 0.5f * Physics.gravity * Time.deltaTime * Time.deltaTime;
            _velocity += Physics.gravity * Time.deltaTime;
        }
        else
        {
            displacement = _direction * _speed * Time.deltaTime;
        }

        float step = displacement.magnitude;
        Vector3 moveDirection = step > 0f ? displacement / step : _direction;
        Vector3 newPos = oldPos + displacement;

        // Start the cast one radius behind, so the bullet still catches something it's already touching.
        Vector3 castOrigin = oldPos - moveDirection * _settings.Radius;
        float castDist = step + _settings.Radius;

        if (Physics.SphereCast(
                castOrigin,
                _settings.Radius,
                moveDirection,
                out RaycastHit hit,
                castDist,
                _settings.BulletCollisionLayer,
                QueryTriggerInteraction.Collide))
        {
            // A hit on the shooter is skipped, and the bullet keeps going.
            if (_damageInfo.DamageSource != null && hit.collider.transform.IsChildOf(_damageInfo.DamageSource.transform))
            {
                transform.position = newPos;
                return;
            }

            GetImpactPoint(hit, moveDirection, out Vector3 impactPoint, out Vector3 impactNormal);

            // Only the collider's own object is checked, so a blocker on a shield doesn't block hits on the rest of the enemy.
            IProjectileBlocker blocker = hit.collider.GetComponent<IProjectileBlocker>();
            if (blocker != null)
            {
                blocker.BlockProjectile(impactPoint, moveDirection);
                SpawnImpact(false, impactPoint, impactNormal);
                OnImpact?.Invoke(impactPoint, impactNormal);
                Destroy(gameObject);
                return;
            }

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(_damageInfo);

                // The object with the IDamageable, such as the enemy root, instead of the child collider that was hit.
                GameObject target = damageable is Component component ? component.gameObject : hit.collider.gameObject;
                _onHit?.Invoke(target, _damageInfo);
            }

            SpawnImpact(damageable != null, impactPoint, impactNormal);
            OnImpact?.Invoke(impactPoint, impactNormal);

            Destroy(gameObject);
            return;
        }

        transform.position = newPos; // Nothing hit, so move forward.
    }

    /// <summary>
    /// The hit point and the surface normal. A cast that starts already touching a collider reports no point or normal,
    /// so that case uses the projectile's position and faces back along its path.
    /// </summary>
    private void GetImpactPoint(RaycastHit hit, Vector3 moveDirection, out Vector3 point, out Vector3 normal)
    {
        bool startedInside = hit.distance <= 0f;
        point = startedInside ? transform.position : hit.point;
        normal = startedInside || hit.normal == Vector3.zero ? -moveDirection : hit.normal;
    }

    /// <summary>Spawns the impact prefab for the kind of thing that was hit, at the hit point, facing out from the surface.</summary>
    private void SpawnImpact(bool hitDamageable, Vector3 point, Vector3 normal)
    {
        GameObject prefab = hitDamageable ? _damageableImpactPrefab : _surfaceImpactPrefab;
        if (prefab == null)
            return;

        Instantiate(prefab, point, Quaternion.LookRotation(normal));
    }
}