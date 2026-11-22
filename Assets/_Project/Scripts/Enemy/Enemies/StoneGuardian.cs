using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// The boss. WaveManager ends the run as a victory when it dies. Picks between its attacks by the player's distance and
/// the current phase, with a weighted random roll and no attack twice in a row when another is ready. Each phase starts
/// below a share of max health. The boss enters a phase change state for Phase Change Duration while its animation
/// plays, then goes back to chasing, faster and attacking more often, with the new phase's attacks unlocked.
/// </summary>
public class StoneGuardian : EnemyBase
{
    [Serializable]
    private class BossAttack
    {
        public EnemyAttack Attack;
        [Tooltip("The first phase that can use this attack. Phase 1 is the start of the fight.")]
        [Min(1)] public int MinPhase = 1;
        [Tooltip("Closest the player can be for this attack, in meters.")]
        [Min(0f)] public float MinDistance;
        [Tooltip("Farthest the player can be for this attack, in meters.")]
        [Min(0f)] public float MaxDistance = 5f;
        [Tooltip("How likely this attack is compared with the other ready ones. Higher is more often.")]
        [Min(0f)] public float Weight = 1f;
    }

    [Serializable]
    private class Phase
    {
        [Tooltip("The phase starts when health drops below this share of max health, such as 0.5 for half.")]
        [Range(0f, 1f)] public float HealthThreshold = 0.5f;
        [Tooltip("Times the move speed during this phase.")]
        [Min(0f)] public float SpeedMultiplier = 1.2f;
        [Tooltip("Times every attack's cooldown during this phase. Below 1 attacks more often.")]
        [Min(0f)] public float CooldownMultiplier = 0.75f;
    }

    [Header("Stone Guardian")]
    [Tooltip("Every attack the boss can use, with when it can use it.")]
    [SerializeField] private List<BossAttack> _bossAttacks = new();
    [Tooltip("The phases after the first, from highest health threshold to lowest. Phase 2 is the first entry.")]
    [SerializeField] private List<Phase> _phases = new() { new Phase() };

    [Header("Phase Change")]
    [Tooltip("Seconds the boss stands still at the start of each new phase, while the animation plays.")]
    [SerializeField, Min(0f)] private float _phaseChangeDuration = 1.5f;
    [Tooltip("Played at the start of each new phase, such as a roar.")]
    [SerializeField, ValueDropdown(nameof(GetStateNames), AppendNextDrawer = true)] private string _phaseChangeAnimation = "PhaseChange";

    private EnemyAttack _lastAttack; // Not picked again while another attack is ready.
    private PhaseChangeState _phaseChangeState;

    /// <summary>The current phase, starting at 1.</summary>
    [ShowInInspector, ReadOnly]
    public int CurrentPhase { get; private set; } = 1;

    /// <summary>The share of max health where each phase after the first starts, from highest to lowest.</summary>
    public IEnumerable<float> PhaseThresholds => _phases.Select(phase => phase.HealthThreshold);

    /// <summary>Fires when a new phase starts, with its number. For the roar sound, the phase change effect and the music.</summary>
    public event Action<int> OnPhaseChanged;

    public override float CooldownScale => CurrentPhase > 1 ? _phases[CurrentPhase - 2].CooldownMultiplier : 1f;

    protected override void Awake()
    {
        base.Awake();
        _phaseChangeState = new PhaseChangeState(this);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        Health.OnDamaged += HandleDamaged;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (Health != null)
            Health.OnDamaged -= HandleDamaged;
    }

    /// <summary>
    /// Picks a ready attack that the current phase allows and that fits the player's distance, weighted by Weight.
    /// Skips the last attack used when another one is ready. Nothing during a phase change.
    /// </summary>
    public override EnemyAttack SelectAttack()
    {
        if (CurrentState == _phaseChangeState || Target == null)
            return null;

        Vector3 toTarget = Target.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        List<BossAttack> ready = new List<BossAttack>();

        foreach (BossAttack option in _bossAttacks)
        {
            if (option.Attack == null || !option.Attack.isActiveAndEnabled || !option.Attack.CanAttack)
                continue;

            if (CurrentPhase < option.MinPhase || distance < option.MinDistance || distance > option.MaxDistance)
                continue;

            ready.Add(option);
        }

        if (ready.Count > 1)
            ready.RemoveAll(option => option.Attack == _lastAttack);

        EnemyAttack picked = PickWeighted(ready);
        if (picked != null)
            _lastAttack = picked;

        return picked;
    }

    /// <summary>Starts the next phase once health drops below its threshold. Skips phases when one hit crosses several.</summary>
    private void HandleDamaged(int currentHealth, GameObject owner)
    {
        if (IsDead || currentHealth <= 0 || Health.MaxHealth <= 0)
            return;

        float healthShare = (float)currentHealth / Health.MaxHealth;
        int newPhase = CurrentPhase;

        while (newPhase - 1 < _phases.Count && healthShare < _phases[newPhase - 1].HealthThreshold)
        {
            newPhase++;
        }

        if (newPhase == CurrentPhase)
            return;

        CurrentPhase = newPhase;
        ChangeState(_phaseChangeState);
        OnPhaseChanged?.Invoke(CurrentPhase);
    }

    private static EnemyAttack PickWeighted(List<BossAttack> options)
    {
        float total = 0f;
        foreach (BossAttack option in options)
        {
            total += option.Weight;
        }

        if (options.Count == 0)
            return null;

        if (total <= 0f)
            return options[Random.Range(0, options.Count)].Attack;

        float roll = Random.value * total;

        foreach (BossAttack option in options)
        {
            roll -= option.Weight;
            if (roll <= 0f)
                return option.Attack;
        }

        return options[options.Count - 1].Attack;
    }

    private IEnumerable<string> GetStateNames()
    {
        EnemyAnimator enemyAnimator = GetComponent<EnemyAnimator>();
        return enemyAnimator != null ? enemyAnimator.GetStateNames() : new[] { "" };
    }

    /// <summary>
    /// Stands still while the phase change animation plays, then goes back to chasing at the new phase's speed. Leaving
    /// AttackState for this state cancels the running attack.
    /// </summary>
    private class PhaseChangeState : IEnemyState
    {
        private readonly StoneGuardian _boss;
        private float _endTime;

        public PhaseChangeState(StoneGuardian boss)
        {
            _boss = boss;
        }

        public void Enter()
        {
            _boss.Movement.StopMoving();
            _boss.PlayAnimation(_boss._phaseChangeAnimation);
            _endTime = Time.time + _boss._phaseChangeDuration;
        }

        public void Tick(float dt)
        {
            if (Time.time >= _endTime)
                _boss.ChangeState(_boss.ChaseState);
        }

        public void Exit()
        {
            _boss.Movement.SpeedMultiplier = _boss._phases[_boss.CurrentPhase - 2].SpeedMultiplier;
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        foreach (BossAttack option in _bossAttacks)
        {
            if (option.Attack != null)
                Gizmos.DrawWireSphere(transform.position, option.MaxDistance);
        }
    }
}