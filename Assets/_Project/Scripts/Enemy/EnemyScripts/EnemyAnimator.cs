using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Plays the enemy's animations from its movement, attacks, and Health. Goes on the enemy root, next to EnemyBase.
/// The Animator needs a Speed float for the Idle, Walk, and Run blend tree, and Attack, Hit, and Dead states.
/// </summary>
public class EnemyAnimator : MonoBehaviour
{
    private const float CrossFadeTime = 0.25f;

    private Animator _animator;
    private NavMeshAgent _agent;
    private EnemyAttack[] _attacks;
    private Health _health;
    private bool _isDead;

    private static readonly int Speed = Animator.StringToHash("Speed");

    private void Awake()
    {
        _agent = GetComponentInChildren<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        _attacks = GetComponentsInChildren<EnemyAttack>();

        _health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        foreach (EnemyAttack attack in _attacks)
        {
            attack.OnAttackWindUpStarted += HandleAttackWindup;
        }

        if (_health != null)
        {
            _health.OnDamaged += HandleDamaged;
            _health.OnDied += HandleDied;
        }
    }

    private void OnDisable()
    {
        foreach (EnemyAttack attack in _attacks)
        {
            attack.OnAttackWindUpStarted -= HandleAttackWindup;
        }

        if (_health != null)
        {
            _health.OnDamaged -= HandleDamaged;
            _health.OnDied -= HandleDied;
        }
    }

    private void Update()
    {
        if (_isDead)
            return;

        _animator.SetFloat(Speed, _agent.velocity.magnitude);
    }

    private void HandleAttackWindup()
    {
        _animator.CrossFadeInFixedTime("Attack", CrossFadeTime);
    }

    /// <summary>Plays Hit, unless an attack is running, so the swing the player sees matches the hit they take.</summary>
    private void HandleDamaged(int current, GameObject owner)
    {
        if (IsAttacking())
            return;

        _animator.CrossFadeInFixedTime("Hit", CrossFadeTime);
    }

    private void HandleDied(GameObject enemy)
    {
        _isDead = true;
        _animator.CrossFadeInFixedTime("Dead", CrossFadeTime);
    }

    private bool IsAttacking()
    {
        foreach (EnemyAttack attack in _attacks)
        {
            if (attack != null && attack.IsAttacking)
                return true;
        }

        return false;
    }
}