using System;
using UnityEngine;
// CHANGED: removed the unused "using System.Net;"

public class Projectile : MonoBehaviour
{
    [SerializeField] private ProjectileSettings _settings;

    private Vector3 _direction;
    private float _travelTime;

  
    private DamageInfo _damageInfo;        
    private float _speed;                   
    private float _range;                    
    private Action<DamageInfo> _onHit;      
    private bool _initialized;             

   
    public float Radius => _settings != null ? _settings.Radius : 0f;

    
    public void Initialize(DamageInfo damageInfo, float speed, float range, Action<DamageInfo> onHit = null)
    {
        _damageInfo = damageInfo;
        _speed = speed;
        _range = range;
        _onHit = onHit;
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
        if (_travelTime * _speed > _range) 
        {
            Destroy(gameObject);
            return;
        }

        Vector3 oldPos = transform.position;
        float step = _speed * Time.deltaTime; 
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
            
            if (_damageInfo.DamageSource != null && hit.collider.transform.IsChildOf(_damageInfo.DamageSource.transform))
            {
                transform.position = newPos;
                return;
            }

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(_damageInfo);
                _onHit?.Invoke(_damageInfo); // CHANGED: lets the gun fire OnDamageDealt
            }

            Destroy(gameObject);
            return;
        }

        // move forward if nothing hit
        transform.position = newPos;
    }
}