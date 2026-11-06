using System;
using System.Collections;
using UnityEngine;

/// <Summary>
/// Base class for enemy attacks.
/// Handles wind-up, cooldown, and cancellation.
/// </Summary>

public abstract class EnemyAttack: MonoBehaviour
{
    protected EnemyBase Owner;
    public bool CanAttack => !IsAttacking && Time.time >= nextAttackTime;
    public bool IsAttacking { get; private set; }
    public event Action OnAttackWindUp;
    private float nextAttackTime;
    private Coroutine attackRoutine;
    
    public void Initialize(EnemyBase owner)
    {
        Owner = owner; 
    }

    public void StartAttack()
    {
        if(!CanAttack || Owner == null || Owner.IsDead)
            return;

        attackRoutine = StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        IsAttacking = true;

        // Announce the wind-up phase
        OnAttackWindUp?.Invoke();

        yield return new WaitForSeconds(Owner.Stats.AttackWindUpTime);
        PerformAttack();

        // Keep attack active for its attack duration
        yield return new WaitForSeconds(AttackDuration);
        FinishAttack();

        IsAttacking = false;

        nextAttackTime = Time.time + Owner.Stats.AttackCooldownTime;

        attackRoutine = null;
    }

    public void CancelAttack()
    {
        if (!IsAttacking)
            return;

        if(attackRoutine != null)
            StopCoroutine(attackRoutine);
        
        attackRoutine = null;
        FinishAttack();

        IsAttacking = false;
        nextAttackTime = Time.time + Owner.Stats.AttackCooldownTime;
    }

    // Each attack decides how to execute its damage.
    protected abstract void PerformAttack();
    protected virtual float AttackDuration => 0f;
    protected virtual void FinishAttack(){}
    protected virtual void OnDisable()
    {
        CancelAttack();
    }
}