using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Controls the enemy's state machine, target detection, attack timing, and death.
/// Connects to EnemyMovement, Health, GoldilocksRange (through IEnemyRange), and LineOfSight (through IEnemyLineOfSight).
/// </summary>
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(Health))]
public class EnemyBase : MonoBehaviour
{
    [Header("References")]
    [Tooltip("What the enemy chases. Leave empty to use the player's CharacterController.")]
    [SerializeField, FormerlySerializedAs("target")] private Transform _target;

    // INTEGRATION: was EnemyStats. Now the shared Stats component, so items and effects can modify enemies the same way
    // as the player, and Health reads its max health from the same component (see Awake).
    [SerializeField, FormerlySerializedAs("stats")] private Stats _stats;

    [Tooltip("Leave empty to find every EnemyAttack in children.")]
    [SerializeField, FormerlySerializedAs("attacks")] private EnemyAttack[] _attacks;

    // INTEGRATION: new. Which stats in _stats hold each value. The properties below read them through ReadStat.
    [Header("Stats")]
    [SerializeField] private StatDefinition _detectionRangeStat;
    [SerializeField] private StatDefinition _minRangeStat;     // Closest the enemy wants to be. Read by GoldilocksRange.
    [SerializeField] private StatDefinition _maxRangeStat;     // Farthest the enemy wants to be. Read by GoldilocksRange.
    [SerializeField] private StatDefinition _moveSpeedStat;    // Read by EnemyMovement every frame.
    [SerializeField] private StatDefinition _attackDamageStat; // Read by MeleeAttack.
    [SerializeField] private StatDefinition _windUpTimeStat;
    [SerializeField] private StatDefinition _cooldownTimeStat;

    /// <summary>Moves the enemy on the NavMesh.</summary>
    public EnemyMovement Movement { get; private set; }

    /// <summary>The enemy's stats.</summary>
    public Stats Stats => _stats;

    /// <summary>The enemy's health. Its OnDied event kills the enemy.</summary>
    public Health Health { get; private set; }

    // INTEGRATION: new. These replace the EnemyStats fields. Each falls back to the old EnemyStats default when the stat
    // isn't set, so prefabs keep working before their stats are filled in.
    /// <summary>How close the target must be to be noticed.</summary>
    public float DetectionRange => ReadStat(_detectionRangeStat, 10f);
    /// <summary>Closest preferred distance to the target.</summary>
    public float MinRange => ReadStat(_minRangeStat, 0f);
    /// <summary>Farthest preferred distance to the target.</summary>
    public float MaxRange => ReadStat(_maxRangeStat, 1f);
    /// <summary>NavMeshAgent speed.</summary>
    public float MoveSpeed => ReadStat(_moveSpeedStat, 5f);
    /// <summary>Damage per attack hit.</summary>
    public float AttackDamage => ReadStat(_attackDamageStat, 10f);
    /// <summary>Seconds between starting an attack and its hit.</summary>
    public float WindUpTime => ReadStat(_windUpTimeStat, 2f);
    /// <summary>Seconds after an attack before the next one.</summary>
    public float CooldownTime => ReadStat(_cooldownTimeStat, 3f);

    /// <summary>Tells whether the target is too close, too far, or in range.</summary>
    public IEnemyRange Range { get; private set; }
    /// <summary>Tells whether the target can be seen.</summary>
    public IEnemyLineOfSight LineOfSight { get; private set; }
    /// <summary>The enemy's attacks.</summary>
    public IReadOnlyList<EnemyAttack> Attacks => _attacks;

    /// <summary>What the enemy chases.</summary>
    public Transform Target => _target;
    /// <summary>True while the target is within detection range and in sight.</summary>
    public bool HasTarget => _isLockedOn && _target != null;
    /// <summary>True once Die has run.</summary>
    public bool IsDead => _isDead;

    /// <summary>True if the LineOfSight component can see the target.</summary>
    public bool HasLineOfSight =>
        LineOfSight?.HasLineOfSight() ?? false;

    /// <summary>Where the target is compared to the preferred distance. TooFar when there's no target.</summary>
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

    /// <summary>Fires once when the enemy dies, with this enemy. RoomEncounter listens to count the enemies left.</summary>
    public event Action<EnemyBase> OnEnemyKilled;

    private IEnemyState _currentState;
    private bool _isLockedOn; // Set every frame in Update.
    private bool _isDead;

    /// <summary>Finds the components, creates the states, and hands each attack this enemy.</summary>
    private void Awake()
    {
        Movement = GetComponent<EnemyMovement>();
        Health = GetComponent<Health>();

        // INTEGRATION: new. Health reads its max health from the same Stats component, the same as on the player.
        if (!_stats) _stats = GetComponent<Stats>();
        if (_stats) Health.SetStats(_stats);

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

        if (_attacks == null || _attacks.Length == 0)
        {
            _attacks = GetComponentsInChildren<EnemyAttack>();
        }

        // Each attack needs its owner to read stats and the target.
        foreach (EnemyAttack attack in _attacks)
        {
            if (attack != null)
            {
                attack.Initialize(this);
            }
        }
    }

    /// <summary>Starts listening for death.</summary>
    private void OnEnable()
    {
        Health.OnDied += HandleDeath;
    }

    /// <summary>Finds the target if none is set, and starts in IdleState.</summary>
    private void Start()
    {
        if (_target == null)
        {
            // INTEGRATION: was FindGameObjectWithTag("Player"). GameManager.Player is the player root, which stays at the
            // spawn point, so the target is the CharacterController's transform, which is the part that moves.
            // LineOfSight and GoldilocksRange read this through Target.
            GameObject player = GameManager.Instance.Player;
            if (player != null)
            {
                CharacterController controller = player.GetComponentInChildren<CharacterController>();
                _target = controller != null ? controller.transform : player.transform;
            }
        }

        // INTEGRATION: removed the "die if health is 0" check here. Health now gets its max from Stats in Awake.
        ChangeState(IdleState);
    }

    /// <summary>Locks on when the target is in range and in sight, and updates the current state.</summary>
    private void Update()
    {
        if (_isDead)
            return;

        if (_target != null)
        {
            float distance = Vector3.Distance(
                transform.position,
                _target.position
            );

            bool inRange = distance <= DetectionRange;
            bool canSeePlayer = inRange && HasLineOfSight;

            _isLockedOn = canSeePlayer;
        }
        else
        {
            _isLockedOn = false;
        }

        // Losing the target sends the enemy back to idle, unless it's stunned.
        if (!HasTarget && _currentState != IdleState && _currentState != StunnedState)
            ChangeState(IdleState);

        _currentState?.Tick(Time.deltaTime);
    }

    /// <summary>Stops listening for death.</summary>
    private void OnDisable()
    {
        if (Health != null)
            Health.OnDied -= HandleDeath;
    }

    /// <summary>Runs when Health fires OnDied.</summary>
    private void HandleDeath(GameObject deadObject)
    {
        Die();
    }

    /// <summary>
    /// Exits the current state and enters a new one. Ignores the change if it's the same state, and once dead, only
    /// DeadState is allowed. DeadState is only allowed through Die.
    /// </summary>
    public void ChangeState(IEnemyState newState)
    {
        if (newState == null || _currentState == newState)
            return;

        if (_isDead && newState != DeadState)
            return;

        if (newState == DeadState && !_isDead)
            return;

        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }

    /// <summary>Interrupts the enemy for a time. Called by parry and other combat systems. Needs a target.</summary>
    public void Stun(float duration)
    {
        if (_isDead || duration <= 0f || !HasTarget)
            return;

        StunnedState.SetDuration(duration);
        ChangeState(StunnedState);
    }

    /// <summary>Kills the enemy: enters DeadState and fires OnEnemyKilled. Runs only once.</summary>
    public void Die()
    {
        if (_isDead)
            return;

        _isDead = true;
        ChangeState(DeadState);
        OnEnemyKilled?.Invoke(this);
    }

    /// <summary>
    /// Returns a stat's value from _stats, or the fallback outside Play mode or when the stat isn't set or found.
    /// </summary>
    // INTEGRATION: new. Used by the stat properties above, which replaced the EnemyStats fields.
    private float ReadStat(StatDefinition stat, float fallback)
    {
        if (!Application.isPlaying || _stats == null || stat == null || !_stats.HasStat(stat))
            return fallback;

        return _stats.GetValue(stat);
    }

    /// <summary>Draws the detection range in the Scene view.</summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            DetectionRange
        );
    }
}
