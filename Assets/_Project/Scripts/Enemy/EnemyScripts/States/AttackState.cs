/// <summary>
/// Manages enemy attack behavior. Checks the Goldilocks range and line of sight, faces the player, and starts the
/// attack EnemyBase.SelectAttack picks whenever no attack is running. Plays CombatIdle between attacks and each
/// attack's own animation while it runs.
/// </summary>
public class AttackState : IEnemyState
{
    private readonly EnemyBase _enemy;
    private EnemyAttack _currentAttack;   // The attack started last. Null until the first attack starts.
    private bool _playingAttack;          // True while an attack's animation is playing, so CombatIdle plays once it ends.
    private bool _previousFacesPath;      // EnemyMovement.FacesPath before Enter, put back in Exit.

    public AttackState(EnemyBase enemy)
    {
        _enemy = enemy;
    }

    /// <summary>Stops moving, plays CombatIdle, and takes over rotation from the NavMeshAgent.</summary>
    public void Enter()
    {
        _enemy.Movement.StopMoving();
        _enemy.PlayStateAnimation(EnemyAnimation.CombatIdle);

        _currentAttack = null;
        _playingAttack = false;

        _previousFacesPath = _enemy.Movement.FacesPath;
        _enemy.Movement.FacesPath = false;
    }

    /// <summary>
    /// Leaves for Idle, Reposition, or Chase when the target is lost, too close, out of sight, or too far.
    /// Otherwise turns toward the target, and starts the next attack once the last one has finished.
    /// </summary>
    public void Tick(float dt)
    {
        if (!_enemy.HasTarget)
        {
            _enemy.ChangeState(_enemy.IdleState);
            return;
        }

        RangeStatus rangeStatus = _enemy.CurrentRange;

        if (rangeStatus == RangeStatus.TooClose || !_enemy.HasLineOfSight)
        {
            _enemy.ChangeState(_enemy.RepositionState);
            return;
        }

        if (rangeStatus == RangeStatus.TooFar)
        {
            _enemy.ChangeState(_enemy.ChaseState);
            return;
        }

        _enemy.FaceTarget(dt);

        if (_currentAttack != null && _currentAttack.IsAttacking)
            return;

        if (_playingAttack)
        {
            _playingAttack = false;
            _enemy.ReturnToStateAnimation();
        }

        EnemyAttack next = _enemy.SelectAttack();
        if (next == null || !next.CanAttack)
            return;

        _currentAttack = next;
        _currentAttack.StartAttack();

        if (_currentAttack.IsAttacking)
        {
            _playingAttack = true;
            _enemy.PlayAnimation(_currentAttack.AnimationState);
        }
    }

    /// <summary>Cancels an unfinished attack and gives rotation back to the NavMeshAgent.</summary>
    public void Exit()
    {
        if (_currentAttack != null)
            _currentAttack.CancelAttack();

        _enemy.Movement.FacesPath = _previousFacesPath;

        _currentAttack = null;
        _playingAttack = false;
    }
}
