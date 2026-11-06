using System;
using System.Collections.Generic;
using UnityEngine;

/// Summary - 
/// Controls the enemy's state machine, target detection,
/// attack timing, and death transitions.
/// Connects to EnemyMovement, Healthbar, Goldilocks,
/// and line-of-sight components.

[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(Health))]
public class EnemyBase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private EnemyStats stats;
    [SerializeField] private EnemyAttack[] attacks;

    public EnemyMovement Movement { get; private set; }
  
    public EnemyStats Stats => stats;
    public Health Health { get; private set; }

    public IEnemyRange Range { get; private set; }
    public IEnemyLineOfSight LineOfSight { get; private set; }
    public IReadOnlyList<EnemyAttack> Attacks => attacks;

    public Transform Target => target;
    public bool HasTarget => isLockedOn && target != null;
    public bool IsDead => isDead;

    public bool HasLineOfSight =>
        LineOfSight?.HasLineOfSight() ?? false;
    // Uses the Goldilocks interface to determine preferred distance.
    public RangeStatus CurrentRange =>
        HasTarget && Range != null
            ? Range.GetRangeStatus()
            : RangeStatus.TooFar;

    public IEnemyState IdleState { get; private set; }
    public IEnemyState ChaseState { get; private set; }
    public IEnemyState RepositionState { get; private set; }
    public IEnemyState AttackState { get; private set; }
    public IEnemyState DeadState { get; private set; }
    public StunnedState StunnedState { get; private set; }
    public event Action<EnemyBase> OnEnemyKilled;
    private IEnemyState currentState;
    private bool isLockedOn;
    private bool isDead;

    // Retrieves components and initializes the five enemy states.
    private void Awake()
    {
        Movement = GetComponent<EnemyMovement>();
        Health = GetComponent<Health>();

        Range = GetComponent(typeof(IEnemyRange)) as IEnemyRange;

        LineOfSight =
            GetComponent(typeof(IEnemyLineOfSight))
            as IEnemyLineOfSight;

        IdleState = new IdleState(this);
        ChaseState = new ChaseState(this);
        RepositionState = new RepositionState(this);
        AttackState = new AttackState(this);
        DeadState = new DeadState(this);
        StunnedState = new StunnedState(this);

        // Find all attack components attached to this enemy
        if(attacks == null || attacks.Length == 0)
        {
            attacks = GetComponentsInChildren<EnemyAttack>();
        }

        // Tell each attack which enemy owns it
        foreach (EnemyAttack attack in attacks)
        {
            if (attack != null)
            {
                attack.Initialize(this);
            }
        }
    }

    // Subscribes to Healthbar's death event.
    private void OnEnable()
    {
        Health.OnDied += HandleDeath;
    }

    // Sets the enemy's initial state based on its health.
    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }
        if (Health.CurrentHealth <= 0f)
            Die();
        else
            ChangeState(IdleState);
    }

    // Detects the player, maintains lock-on, and updates the active state.
    private void Update()
    {
        if (isDead)
            return;

        if (target != null)
        {
            float distance = Vector3.Distance(
                transform.position,
                target.position
            );

            bool inRange = distance <= stats.DetectionRange;
            bool canSeePlayer = inRange && HasLineOfSight;

            isLockedOn = canSeePlayer;
        }
        else
        {
            isLockedOn = false;
        }

        if(!HasTarget && currentState != IdleState && currentState != StunnedState)
            ChangeState(IdleState);

        currentState?.Tick(Time.deltaTime);
    }

    // Unsubscribes from Healthbar's death event when disabled.
    private void OnDisable()
    {
        if (Health != null)
            Health.OnDied -= HandleDeath;
    }

    // Handles the death notification received from Healthbar.
    private void HandleDeath(GameObject deadObject)
    {
        Die();
    }

    // Exits the current state and enters the new state.
    // Prevents invalid transitions and leaving DeadState.
    public void ChangeState(IEnemyState newState)
    {
        if (newState == null || currentState == newState)
            return;

        if (isDead && newState != DeadState)
            return;

        if (newState == DeadState && !isDead)
            return;

        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }
    // Called by melee parry/combat systems to interupt the current fighting state.
    public void Stun(float duration)
    {
        if(isDead || duration <= 0f || !HasTarget)
            return;
        
        StunnedState.SetDuration(duration);
        ChangeState(StunnedState);
    }
    // Marks the enemy as dead and enters the terminal DeadState.
    public void Die()
    {
        if (isDead)
            return;

        isDead = true;
        ChangeState(DeadState);
        OnEnemyKilled?.Invoke(this);
    }

    // Visualizes the detection radius in Unity's Scene view.
    private void OnDrawGizmosSelected()
    {
        if (stats == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            stats.DetectionRange
        );
    }
}