using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

/// <summary>
/// Base class for enemy attacks. Runs one attack as wind-up, hit, active time, recovery, then cooldown, and can cancel it.
/// AttackState starts the attack EnemyBase.SelectAttack picks, plays its Animation State, and calls CancelAttack when it
/// exits. Damage, wind-up, and cooldown come from the enemy's stats, times this attack's multipliers, so one enemy's
/// attacks can differ, such as the Dark Knight's quick and overhead swings.
/// </summary>
public abstract class EnemyAttack : MonoBehaviour
{
    [Header("Attack")]
    [Tooltip("The Animator state AttackState plays when this attack starts. It should cover the wind-up and the hit. The dropdown lists the states in the enemy's Animator Controller.")]
    [SerializeField, ValueDropdown(nameof(GetStateNames), AppendNextDrawer = true)] private string _animationState = "Attack";
    [Tooltip("Times the enemy's Attack Damage stat.")]
    [SerializeField, Min(0f)] private float _damageMultiplier = 1f;
    [Tooltip("Times the enemy's Wind Up Time stat. Above 1 makes a slower, easier to dodge attack.")]
    [SerializeField, Min(0f)] private float _windUpMultiplier = 1f;
    [Tooltip("Times the enemy's Cooldown Time stat. The cooldown starts after the recovery.")]
    [SerializeField, Min(0f)] private float _cooldownMultiplier = 1f;
    [Tooltip("Seconds the attack keeps running after the hit, so the rest of the animation plays before the enemy goes back to CombatIdle. Set it to the clip's length minus the time the hit ends.")]
    [SerializeField, Min(0f)] private float _recoveryTime;
    [SerializeField] private string _attackSoundName;
    [SerializeField] private string _windUpSoundName;

    private float _nextAttackTime;     // Time.time when the cooldown ends.
    private Coroutine _attackRoutine;  // The running AttackRoutine, kept so CancelAttack can stop it.

    /// <summary>The enemy this attack belongs to. Set by EnemyBase.Awake through Initialize.</summary>
    protected EnemyBase Owner { get; private set; }

    public string AnimationState => _animationState;

    /// <summary>True when no attack is running and the cooldown has passed.</summary>
    public bool CanAttack => !IsAttacking && Time.time >= _nextAttackTime;

    /// <summary>True from the start of the wind-up until the recovery ends or the attack is canceled.</summary>
    public bool IsAttacking { get; private set; }

    /// <summary>This attack's damage: the enemy's Attack Damage stat times Damage Multiplier, rounded.</summary>
    protected int Damage => Mathf.RoundToInt(Owner.AttackDamage * _damageMultiplier);

    /// <summary>Seconds before this attack can start again: the Cooldown Time stat times Cooldown Multiplier and the enemy's CooldownScale.</summary>
    private float Cooldown => Owner.CooldownTime * _cooldownMultiplier * Owner.CooldownScale;

    /// <summary>Fires when an attack's wind-up starts. DarkKnight counts its swings from it.</summary>
    public event Action OnAttackWindUpStarted;

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

    /// <summary>Waits the wind-up, hits, waits the active time, finishes, waits the recovery, then starts the cooldown.</summary>
    private IEnumerator AttackRoutine()
    {
        IsAttacking = true;

        StartWindUp();
        OnAttackWindUpStarted?.Invoke();

        yield return new WaitForSeconds(Owner.WindUpTime * _windUpMultiplier);
        PerformAttack();

        // Keeps the attack active for its duration, such as a melee hitbox staying on.
        yield return new WaitForSeconds(AttackDuration);
        FinishAttack();

        // The rest of the animation plays out. The enemy still faces the player but doesn't start another attack.
        if (_recoveryTime > 0f)
            yield return new WaitForSeconds(_recoveryTime);

        IsAttacking = false;
        _nextAttackTime = Time.time + Cooldown;
        _attackRoutine = null;
    }

    /// <summary>Stops a running attack, ends it, and starts the cooldown. Only acts while an attack is running.</summary>
    public void CancelAttack()
    {
        if (!IsAttacking)
            return;

        if (_attackRoutine != null)
            StopCoroutine(_attackRoutine);

        _attackRoutine = null;
        FinishAttack();

        IsAttacking = false;
        _nextAttackTime = Time.time + Cooldown;
    }

    /// <summary>Runs when the wind-up starts, before OnAttackWindUpStarted, such as to show a telegraph.</summary>
    protected virtual void StartWindUp() 
    {
        if(!_windUpSoundName.IsNullOrWhitespace())
        {
            SoundController.Instance.PlaySFX(_windUpSoundName, gameObject.transform.position);
        }
    }

    /// <summary>Deals the attack's damage. Each attack type decides how.</summary>
    protected virtual void PerformAttack()
    {
        SoundController.Instance.PlaySFX(_attackSoundName, gameObject.transform.position);
    }

    /// <summary>Seconds the attack stays active after PerformAttack. 0 by default.</summary>
    protected virtual float AttackDuration => 0f;

    /// <summary>Ends the attack, such as turning a hitbox off. Runs when the attack finishes or is canceled.</summary>
    protected virtual void FinishAttack() { }

    protected virtual void OnDisable()
    {
        CancelAttack();
    }

    private IEnumerable<string> GetStateNames()
    {
        EnemyAnimator enemyAnimator = GetComponentInParent<EnemyAnimator>(true);
        return enemyAnimator != null ? enemyAnimator.GetStateNames() : new[] { "" };
    }
}