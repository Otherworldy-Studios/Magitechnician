using UnityEngine;

/// <summary>
/// The close-range enemy. Has two melee attacks: a quick sideways swing it uses by default, and a slow overhead swing
/// it uses after a number of quick swings in a row, or once the player has stayed in range for a while. Blocks bullets
/// with BlockReaction, and doesn't attack while blocking.
/// </summary>
[RequireComponent(typeof(BlockReaction))]
public class DarkKnight : EnemyBase
{
    [Header("Dark Knight")]
    [Tooltip("The default attack. Give it a short Wind Up Multiplier.")]
    [SerializeField] private MeleeAttack _quickSwing;
    [Tooltip("The heavy attack. Give it a long Wind Up Multiplier and a high Damage Multiplier.")]
    [SerializeField] private MeleeAttack _overheadSwing;
    [Tooltip("Quick swings in a row before the overhead swing.")]
    [SerializeField, Min(1)] private int _quickSwingsBeforeOverhead = 2;
    [Tooltip("Seconds the player has to stay in range before the overhead swing, even without enough quick swings.")]
    [SerializeField, Min(0f)] private float _overheadAfterSecondsInRange = 4f;

    private BlockReaction _blockReaction;
    private int _quickSwingsInARow;
    private float _secondsInRange;

    protected override void Awake()
    {
        base.Awake();
        _blockReaction = GetComponent<BlockReaction>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (_quickSwing != null) _quickSwing.OnAttackWindUpStarted += HandleQuickSwingStarted;
        if (_overheadSwing != null) _overheadSwing.OnAttackWindUpStarted += HandleOverheadSwingStarted;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (_quickSwing != null) _quickSwing.OnAttackWindUpStarted -= HandleQuickSwingStarted;
        if (_overheadSwing != null) _overheadSwing.OnAttackWindUpStarted -= HandleOverheadSwingStarted;
    }

    protected override void Update()
    {
        base.Update();

        if (IsDead)
            return;

        _secondsInRange = HasTarget && CurrentRange == RangeStatus.InRange ? _secondsInRange + Time.deltaTime : 0f;
    }

    /// <summary>
    /// Picks the overhead swing when it's ready and the Dark Knight has swung quickly enough times or the player has
    /// stayed in range long enough. Otherwise the quick swing. Nothing while blocking or while both are cooling down.
    /// </summary>
    public override EnemyAttack SelectAttack()
    {
        if (_blockReaction != null && _blockReaction.IsBlocking)
            return null;

        if (_quickSwing == null && _overheadSwing == null)
            return base.SelectAttack();

        bool wantsOverhead = _quickSwingsInARow >= _quickSwingsBeforeOverhead || _secondsInRange >= _overheadAfterSecondsInRange;

        if (wantsOverhead && IsReady(_overheadSwing))
            return _overheadSwing;

        return IsReady(_quickSwing) ? _quickSwing : null;
    }

    private static bool IsReady(EnemyAttack attack)
    {
        return attack != null && attack.isActiveAndEnabled && attack.CanAttack;
    }

    private void HandleQuickSwingStarted()
    {
        _quickSwingsInARow++;
    }

    private void HandleOverheadSwingStarted()
    {
        _quickSwingsInARow = 0;
        _secondsInRange = 0f;
    }
}
