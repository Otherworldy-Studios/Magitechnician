using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Base class for enemy attacks. Runs one attack as wind-up, hit, active time, then cooldown, and can cancel it.
/// AttackState calls StartAttack when CanAttack is true, and CancelAttack when it exits.
/// </summary>
public abstract class EnemyAttack : MonoBehaviour
{
    /// <summary>The enemy this attack belongs to. Set by EnemyBase.Awake through Initialize.</summary>
    protected EnemyBase Owner { get; private set; }

    /// <summary>True when no attack is running and the cooldown has passed.</summary>
    public bool CanAttack => !IsAttacking && Time.time >= _nextAttackTime;

    /// <summary>True from the start of the wind-up until the attack finishes or is canceled.</summary>
    public bool IsAttacking { get; private set; }

    /// <summary>Fires when an attack's wind-up starts. For warning animations and sounds.</summary>
    // Nothing listens yet.
    public event Action OnAttackWindUpStarted;

    private float _nextAttackTime;     // Time.time when the cooldown ends.
    private Coroutine _attackRoutine;  // The running AttackRoutine, kept so CancelAttack can stop it.

    /// <summary>Sets the owning enemy. EnemyBase.Awake calls this for each of its attacks.</summary>
    public void Initialize(EnemyBase owner)
    {
        Owner = owner;
    }

    /// <summary>Starts an attack if CanAttack is true and the owner is alive.</summary>
    public void StartAttack()
    {
        if (!CanAttack || Owner == null || Owner.IsDead)
            return;

        _attackRoutine = StartCoroutine(AttackRoutine());
    }

    /// <summary>Waits the wind-up, hits, waits the active time, then finishes and starts the cooldown.</summary>
    private IEnumerator AttackRoutine()
    {
        IsAttacking = true;

        OnAttackWindUpStarted?.Invoke();

        // INTEGRATION: was Owner.Stats.AttackWindUpTime from EnemyStats. EnemyBase.WindUpTime reads the Stats component.
        yield return new WaitForSeconds(Owner.WindUpTime);
        PerformAttack();

        // Keeps the attack active for its duration, such as a melee hitbox staying on.
        yield return new WaitForSeconds(AttackDuration);
        FinishAttack();

        IsAttacking = false;

        _nextAttackTime = Time.time + Owner.CooldownTime;

        _attackRoutine = null;
    }

    /// <summary>Stops a running attack, ends it, and starts the cooldown. Does nothing if no attack is running.</summary>
    public void CancelAttack()
    {
        if (!IsAttacking)
            return;

        if (_attackRoutine != null)
            StopCoroutine(_attackRoutine);

        _attackRoutine = null;
        FinishAttack();

        IsAttacking = false;
        _nextAttackTime = Time.time + Owner.CooldownTime;
    }

    /// <summary>Deals the attack's damage. Each attack type decides how.</summary>
    protected abstract void PerformAttack();

    /// <summary>Seconds the attack stays active after PerformAttack. 0 by default.</summary>
    protected virtual float AttackDuration => 0f;

    /// <summary>Ends the attack, such as turning a hitbox off. Runs when the attack finishes or is canceled.</summary>
    protected virtual void FinishAttack() { }

    /// <summary>Cancels a running attack when the object is turned off.</summary>
    protected virtual void OnDisable()
    {
        CancelAttack();
    }
}
