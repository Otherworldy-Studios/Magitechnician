using UnityEngine;
using UnityEngine.AI;

public class EnemyAnimator : MonoBehaviour
{
    private Animator _animator;
    private NavMeshAgent _agent;
    private EnemyAttack[] _attacks;
    Health _health;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (EnemyAttack attack in _attacks)
        {
            attack.OnAttackWindUpStarted += HandleAttackWindup;
        }



        EnemyBase enemyBase = GetComponent<EnemyBase>();
        enemyBase.Health.OnDamaged += HandleDamaged;

        enemyBase.Health.OnDied += HandleDied;
    }

    // Update is called once per frame
    void Update()
    {
        _animator.SetFloat("Speed", _agent.velocity.magnitude);

        if (_animator.GetFloat("Speed") > 2.0f)
        {
            _animator.CrossFadeInFixedTime("Run", 0.25f); //Run state
        }
        else if (_animator.GetFloat("Speed") > 0.1f)
        {
            _animator.CrossFadeInFixedTime("Walk", 0.25f); //Walk state
        }
        else
        {
            _animator.CrossFadeInFixedTime("Idle", 0.25f); //Idle state
        }


    }

    void OnDisable()
    {
        foreach (EnemyAttack attack in _attacks)
        {
            attack.OnAttackWindUpStarted -= HandleAttackWindup;
        }

        EnemyBase enemyBase = GetComponent<EnemyBase>();
        enemyBase.Health.OnDamaged -= HandleDamaged;

        enemyBase.Health.OnDied -= HandleDied;
    }

    private void HandleAttackWindup()
    {
        _animator.CrossFadeInFixedTime("Attack", 0.25f); //Attack state

    }

    private void HandleDamaged(int current, GameObject owner)
    {
        _animator.CrossFadeInFixedTime("Hit", 0.25f); //Hit state
    }

    private void HandleDied(GameObject enemy)
    {
        _animator.CrossFadeInFixedTime("Dead", 0.25f); //Dead state
    }

    private void HandleStunned()
    {
        _animator.CrossFadeInFixedTime("Stunned", 0.25f); //Stunned state
    }
    
}
