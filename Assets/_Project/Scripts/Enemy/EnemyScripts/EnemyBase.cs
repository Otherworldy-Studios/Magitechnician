
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

    [Header("Detection")]
    [SerializeField, Min(0f)]
    private float detectionRange = 10f;

    public EnemyMovement Movement { get; private set; }
    public Health Health { get; private set; }

    public IEnemyRange Range { get; private set; }
    public IEnemyLineOfSight LineOfSight { get; private set; }

    public Transform Target => target;
    public bool HasTarget => isLockedOn && target != null;
    public bool IsDead => isDead;

    // Temporary fallback until the LOS system is implemented.
    public bool HasLineOfSight =>
        LineOfSight?.HasLineOfSight() ?? true;
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

    private IEnemyState currentState;
    private bool isLockedOn;
    private bool isDead;

    // Retrieves components and initializes the five enemy states.
    private void Awake()
    {
        Movement = GetComponent<EnemyMovement>();
        Health = GetComponent<Health>();

        // TODO: Connect Goldilocks component.
        Range = GetComponent(typeof(IEnemyRange)) as IEnemyRange;

        // TODO: Connect line-of-sight component.
        LineOfSight =
            GetComponent(typeof(IEnemyLineOfSight))
            as IEnemyLineOfSight;

        IdleState = new IdleState(this);
        ChaseState = new ChaseState(this);
        RepositionState = new RepositionState(this);
        AttackState = new AttackState(this);
        DeadState = new DeadState(this);
    }

    // Subscribes to Healthbar's death event.
    private void OnEnable()
    {
        Health.OnDied += HandleDeath;
    }

    // Sets the enemy's initial state based on its health.
    private void Start()
    {
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

        if (!isLockedOn && target != null)
        {
            float distance = Vector3.Distance(
                transform.position,
                target.position
            );

            if (distance <= detectionRange)
                isLockedOn = true;
        }

        currentState?.Update();
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

    // Marks the enemy as dead and enters the terminal DeadState.
    public void Die()
    {
        if (isDead)
            return;

        isDead = true;
        ChangeState(DeadState);
    }

    // Visualizes the detection radius in Unity's Scene view.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );
    }
}