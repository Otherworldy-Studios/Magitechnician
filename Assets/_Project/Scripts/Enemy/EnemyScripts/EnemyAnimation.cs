/// <summary>
/// The animations the enemy states ask for. EnemyAnimator maps each one to an Animator state name, set per enemy in
/// the Inspector, so each enemy's Animator Controller can name its states however it likes.
/// </summary>
public enum EnemyAnimation
{
    Idle,
    Chase,
    Reposition,
    CombatIdle,
    StunnedBegin,
    StunnedEnd,
    Dead,
    Hit,
    Block
}