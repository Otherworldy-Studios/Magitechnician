using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

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
    [SerializeField] private Transform _target;

    // The shared Stats component, so items and effects modify enemies the same way as the player.
    // Health reads its max health from it too (see Awake).
    [Tooltip("Leave empty to use the Stats on this object.")]
    [SerializeField] private Stats _stats;

    [Tooltip("Leave empty to find every EnemyAttack in children.")]
    [SerializeField] private EnemyAttack[] _attacks;

    // Which stats in _stats hold each value. The properties below read them through ReadStat.
    [Header("Stats")]
    [SerializeField] private StatDefinition _detectionRangeStat;
    [SerializeField] private StatDefinition _minRangeStat;     // Closest the enemy wants to be. Read by GoldilocksRange.
    [SerializeField] private StatDefinition _maxRangeStat;     // Farthest the enemy wants to be. Read by GoldilocksRange.
    [SerializeField] private StatDefinition _moveSpeedStat;    // Read by EnemyMovement every frame.
    [SerializeField] private StatDefinition _attackDamageStat; // Read by MeleeAttack.
    [SerializeField] private StatDefinition _windUpTimeStat;
    [SerializeField] private StatDefinition _cooldownTimeStat;

    [Header("Death")]
    [Tooltip("Seconds to wait before removing the enemy after death, so the death animation can play.")]
    [SerializeField, Min(0f)] private float _deathDelay = 2f;

    private IEnemyState _currentState;
    private bool _isLockedOn; // Set every frame in Update.
    private bool _isDead;
    private bool _isAggressive;

    /// <summary>Fires once when the enemy dies, with this enemy. WaveManager listens to count the enemies left.</summary>
    public event Action<EnemyBase> OnEnemyKilled;

    public EnemyMovement Movement { get; private set; }
    public Health Health { get; private set; }
    public Stats Stats => _stats;
    public float DeathDelay => _deathDelay;

    // Each stat falls back to a default when it isn't set, so prefabs keep working before their stats are filled in.
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
    public IEnemyLineOfSight LineOfSight { get; private set; }
    public IReadOnlyList<EnemyAttack> Attacks => _attacks;

    public Transform Target => _target;
    /// <summary>True while the enemy is locked on: in range and in sight, or aggressive.</summary>
    public bool HasTarget => _isLockedOn && _target != null;
    public bool IsDead => _isDead;
    public bool HasLineOfSight => LineOfSight?.HasLineOfSight() ?? false;

    /// <summary>Where the target is compared to the preferred distance. TooFar when there's no target.</summary>
    public RangeStatus CurrentRange => HasTarget && Range != null ? Range.GetRangeStatus() : RangeStatus.TooFar;

    public IEnemyState IdleState { get; private set; }
    public IEnemyState ChaseState { get; private set; }
    public IEnemyState RepositionState { get; private set; }
    public IEnemyState AttackState { get; private set; }
    public IEnemyState DeadState { get; private set; }
    public StunnedState StunnedState { get; private set; }

    private void Awake()
    {
        Movement = GetComponent<EnemyMovement>();
        Health = GetComponent<Health>();

        if (!_stats) _stats = GetComponent<Stats>();
        if (_stats) Health.SetStats(_stats);

        Range = GetComponent(typeof(IEnemyRange)) as IEnemyRange;
        LineOfSight = GetComponent(typeof(IEnemyLineOfSight)) as IEnemyLineOfSight;

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

    private void OnEnable()
    {
        Health.OnDied += HandleDeath;
    }

    private void Start()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnPlayerRegistered += HandlePlayerRegistered;

            if (_target == null)
            {
                GameObject player = GameManager.Instance.Player;
                if (player != null)
                {
                    PlayerRoot playerRoot = player.GetComponent<PlayerRoot>();
                    if (playerRoot != null)
                        HandlePlayerRegistered(playerRoot);
                }
            }
        }

        ChangeState(IdleState);

        if (_isAggressive && HasTarget)
            ChangeState(ChaseState);
    }

    private void Update()
    {
        if (_isDead)
            return;

        if (_isAggressive)
        {
            _isLockedOn = _target != null;
        }
        else if (_target != null)
        {
            float distance = Vector3.Distance(transform.position, _target.position);

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

    private void OnDisable()
    {
        if (Health != null)
            Health.OnDied -= HandleDeath;
    }

    private void OnDestroy()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnPlayerRegistered -= HandlePlayerRegistered;
        }
    }

    /// <summary>
    /// Targets the player's CharacterController, which is the part of the player that moves. The PlayerRoot stays where
    /// the prefab was placed. Locks on right away if the enemy is aggressive.
    /// </summary>
    private void HandlePlayerRegistered(PlayerRoot player)
    {
        if (player == null)
            return;

        CharacterController controller = player.GetComponentInChildren<CharacterController>();

        _target = controller != null ? controller.transform : player.transform;

        if (_isAggressive && !_isDead)
        {
            _isLockedOn = true;
            ChangeState(ChaseState);
        }
    }

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

    /// <summary>Interrupts the enemy for a time. Needs a target.</summary>
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
    /// Locks on to the player without waiting for the detection range or line of sight, and starts chasing.
    /// WaveManager calls this on every enemy it spawns.
    /// </summary>
    [Button("Make Aggressive")]
    public void MakeAggressive()
    {
        if (!Application.isPlaying || _isDead)
            return;

        _isAggressive = true;

        if (_target == null && GameManager.HasInstance)
        {
            GameObject player = GameManager.Instance.Player;
            if (player != null)
            {
                CharacterController controller = player.GetComponentInChildren<CharacterController>();
                _target = controller != null ? controller.transform : player.transform;
            }
        }

        _isLockedOn = _target != null;

        if (HasTarget)
            ChangeState(ChaseState);
    }

    /// <summary>
    /// Returns a stat's value from _stats, or the fallback outside Play mode or when the stat isn't set or found.
    /// </summary>
    private float ReadStat(StatDefinition stat, float fallback)
    {
        if (!Application.isPlaying || _stats == null || stat == null || !_stats.HasStat(stat))
            return fallback;

        return _stats.GetValue(stat);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, DetectionRange);
    }
}