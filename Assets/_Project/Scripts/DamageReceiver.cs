using UnityEngine;

public class DamageReceiver : MonoBehaviour, IDamageable
{
    [SerializeField] private Healthbar _health;
    [SerializeField] private ShieldSystem _shield;

    private void Awake()
    {
        if(!_health)
            _health = GetComponent<Healthbar>();

        if (!_shield)
            _shield = GetComponent<ShieldSystem>();
    }
    
    public void TakeDamage(DamageInfo  damageInfo)
    {
        if (!_health && !_shield)
            return;
        
        DamageInfo info = damageInfo;
        
        if (_shield)
        {
            info.DamageAmount = _shield.AbsorbDamageAndReturnOverflow(damageInfo);
        }

        if (_health && info.DamageAmount > 0)
        {
            _health.TakeDamage(info);
        }
    }
}
