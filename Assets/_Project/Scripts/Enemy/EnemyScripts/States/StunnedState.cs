using UnityEngine;

/// <summary>
/// Represents the state of an enemy when it is stunned. The enemy is unable to move or attack while in this state.
/// Plays StunnedBegin, which the Animator moves on to a looping stun by itself. When the stun runs out, plays
/// StunnedEnd and waits for it to finish before going to Chase or Idle.
/// </summary>
public class StunnedState : IEnemyState
{
    private const float MaxRecoverySeconds = 5f; // Leaves even if StunnedEnd never reports finished, such as a looping clip.

    private readonly EnemyBase _enemy;
    private float _remainingTime;   // Seconds of stun left. Set by SetDuration.
    private bool _recovering;       // True while StunnedEnd plays.
    private float _recoveryElapsed; // Seconds since StunnedEnd started.

    public StunnedState(EnemyBase enemy)
    {
        _enemy = enemy;
    }

    /// <summary>
    /// Sets how long the stun lasts. EnemyBase.Stun calls this before entering the state. A new stun during StunnedEnd
    /// plays StunnedBegin again.
    /// </summary>
    public void SetDuration(float duration)
    {
        _remainingTime = Mathf.Max(0f, duration);

        if (_recovering)
        {
            _recovering = false;
            _enemy.PlayStateAnimation(EnemyAnimation.StunnedBegin);
        }
    }

    /// <summary>Stops moving and plays StunnedBegin.</summary>
    public void Enter()
    {
        _recovering = false;
        _enemy.Movement.StopMoving();
        _enemy.PlayStateAnimation(EnemyAnimation.StunnedBegin);
    }

    /// <summary>
    /// Counts down the stun, then plays StunnedEnd. Once it finishes, goes to Chase if there's a target, or Idle if not.
    /// </summary>
    public void Tick(float dt)
    {
        if (!_recovering)
        {
            _remainingTime -= dt;
            if (_remainingTime > 0f)
                return;

            _recovering = true;
            _recoveryElapsed = 0f;
            _enemy.PlayStateAnimation(EnemyAnimation.StunnedEnd);
            return;
        }

        _recoveryElapsed += dt;

        bool finished = _enemy.Animations == null || _enemy.Animations.HasFinished(EnemyAnimation.StunnedEnd);
        if (!finished && _recoveryElapsed < MaxRecoverySeconds)
            return;

        _enemy.ChangeState(_enemy.HasTarget ? _enemy.ChaseState : _enemy.IdleState);
    }

    public void Exit()
    {
        _recovering = false;
    }
}