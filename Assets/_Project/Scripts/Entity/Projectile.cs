using System.Net;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private ProjectileSettings _settings;

    private Vector3 _direction;
    private float _travelTime;
    /*DamageInfo _damageInfo;

    private DamageInfo _damageInfo;
    private bool _initialized;

    public void Initialize(GameObject attacker)
    {
        _damageInfo.attacker = attacker;
        _damageInfo.Damage = _settings.Damage;
        _initialized = true;
    }*/

    private void Start()
    {
        /*if (!_initialized)
        {
            Debug.LogError("Projectile was not initialized!");
            Destroy(gameObject);
            return;
        }*/

        _direction = transform.forward.normalized;
        Destroy(gameObject, 5f); // Just in case
    }

    /// <summary>
    /// Moves the projectile forward and checks for collisions using a SphereCast
    /// to prevent missing objects between frames.
    /// Destroys the projectile when it hits something or exceeds its maximum range.
    /// </summary>
    private void Update()
    {
        _travelTime += Time.deltaTime;
        if (_travelTime * _settings.Speed > _settings.MaxDistance)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 oldPos = transform.position;
        float step = _settings.Speed * Time.deltaTime;
        Vector3 newPos = oldPos + _direction * step;

        // start a radius behind so we still catch things we're already brushing
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
             /*if (_damageInfo.Owner != null && hit.collider.transform.IsChildOf(_damageInfo.Owner.transform))
             {
                 transform.position = newPos;
                 return;
             }

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

             if (damageable != null)
             {
                 damageable.TakeDamage(_damageInfo);
                 Debug.Log($"Hit damageable: {hit.collider.name}");
             }*/

            Destroy(gameObject);
            return;
        }

        // move forward if nothing hit
        transform.position = newPos;
    }
}