#if UNITY_EDITOR || DEVELOPMENT_BUILD

using UnityEngine;
using UnityEngine.InputSystem;
public class HealthDebug : MonoBehaviour
{
    [Header("Instant heal")]
    [SerializeField] private float _heal;
    
    [Header("Instant damage")]
    [SerializeField] private float _damage;
    
    [Header("Heal over time")]
    [SerializeField] private float _healOverTime;
    [SerializeField] private float _healTimer;

    [Header("Damage over time")]
    [SerializeField] private float _damageOverTime;
    [SerializeField] private float _damageTimer;

    [Space(5)] [SerializeField] private bool _apply;
    
    [SerializeField] private Healthbar _health;
    [SerializeField] private PlayerMovement _playerMovement;
    
    [Header("Player Debug Controls")]
    [TextArea(5, 8)]
    [SerializeField] private string _instructions =
        "F1 - Heal 25 HP\n" +
        "F2 - Deal 25 damage\n" +
        "F3 - Toggle 5 damage over time\n" +
        "F4 - Toggle 5 healing over time";

    private bool _isDamageOverTime = false;
    private bool _isHealOverTime = false;
    

    private void Awake()
    {
        if (!_health)
            _health = GetComponent<Healthbar>();
        if (!_playerMovement)
            _playerMovement = GetComponent<PlayerMovement>();
    }
    
    private void Update()
    {
        if (!_health)
            return;
        
        AnyEntityDebug();
        PlayerDebug();
    }

    private void AnyEntityDebug()
    {
        if (_heal > 0f && _apply)
        {
            _health.RegenHealth(_heal);
            _heal = 0f;
            _apply = false;
        }

        if (_damage > 0f && _apply)
        {
            _health.TakeDamage(new DamageInfo(){ DamageAmount = _damage});
            _damage = 0f;
            _apply = false;
        }

        if (_damageOverTime > 0f && _damageTimer > 0f && _apply)
        {
            _health.TakeDamageOverTime(new DamageInfo(){DamageAmount = _damageOverTime}, _damageTimer);
            _damageOverTime = 0f;
            _damageTimer = 0f;
            _apply = false;
        }

        if (_healOverTime > 0f && _healTimer > 0f && _apply)
        {
            _health.RegenHealthOverTime(_healOverTime, _healTimer);
            _healOverTime = 0f;
            _healTimer = 0f;
            _apply = false;
        }
    }
    
    private void PlayerDebug()
    {
        if(!_playerMovement)
            return;
        
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            _health.RegenHealth(25);
        }

        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            _health.TakeDamage(new DamageInfo(){ DamageAmount = 25f});
        }

        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            _isDamageOverTime = !_isDamageOverTime;

            if (_isDamageOverTime)
            {
                _health.TakeDamageOverTime(
                    new DamageInfo() { DamageAmount = 50f }, 10f);
            }
            else
            {
                _health.TakeDamageOverTime(
                    new DamageInfo() { DamageAmount = 0f }, 0f);
            }
        }

        if (Keyboard.current.f4Key.wasPressedThisFrame)
        {
            _isHealOverTime = !_isHealOverTime;
            
            if (_isHealOverTime)
            {
                _health.RegenHealthOverTime(50f, 10f);
            } 
            else
            {
                _health.RegenHealthOverTime(0f, 0f);
            }
        }
    }
}
#endif