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

    }

    // Update is called once per frame
    void Update()
    {
        _animator.SetFloat("Speed", _agent.velocity.magnitude);
    }

    private void HandleAttackWindup()
    {

    }

    private void HandleDamaged(int current, GameObject owner)
    {

    }

    private void HandleDied(GameObject enemy)
    {
        
    }
    
}
