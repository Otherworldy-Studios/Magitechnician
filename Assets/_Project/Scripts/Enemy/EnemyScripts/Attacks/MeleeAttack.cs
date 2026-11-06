using UnityEngine;

/// <summary>
/// Performs a melee attack using a hitbox.
/// </summary>
public class MeleeAttack : EnemyAttack
{
    [SerializeField] private EnemyMeleeHitbox _hitbox;
    [SerializeField] private int _damage = 10;
    [SerializeField] private float _activeTime = 0.25f;

    protected override float AttackDuration => _activeTime;

    protected override void PerformAttack()
    {
        if (_hitbox == null)
            return;

        DamageInfo info = new DamageInfo
        {
            DamageAmount = _damage,
            DamageSource = Owner.gameObject,
            DamageDirection = Owner.transform.forward,
            DamageType = DamageType.Physical
        };

        _hitbox.Activate(info);
    }

    protected override void FinishAttack()
    {
        if (_hitbox != null)
            _hitbox.Deactivate();
    }
}