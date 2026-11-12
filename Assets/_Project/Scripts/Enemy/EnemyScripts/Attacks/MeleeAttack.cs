using UnityEngine;

/// <summary>
/// A melee attack. When the wind-up ends, turns on an EnemyMeleeHitbox for _activeTime seconds.
/// </summary>
public class MeleeAttack : EnemyAttack
{
    [SerializeField] private EnemyMeleeHitbox _hitbox;

    // INTEGRATION: removed the _damage field. Damage now comes from EnemyBase.AttackDamage, which reads the Stats component.

    [Tooltip("Seconds the hitbox stays on after the wind-up.")]
    [SerializeField] private float _activeTime = 0.25f;

    /// <summary>How long the hitbox stays on. EnemyAttack waits this long before FinishAttack.</summary>
    protected override float AttackDuration => _activeTime;

    /// <summary>Builds the damage and turns the hitbox on with it.</summary>
    protected override void PerformAttack()
    {
        if (_hitbox == null)
            return;

        DamageInfo info = new DamageInfo
        {
            DamageAmount = Mathf.RoundToInt(Owner.AttackDamage), // INTEGRATION: was _damage. Rounded because stats are floats.
            DamageSource = Owner.gameObject,
            DamageDirection = Owner.transform.forward,
            DamageType = DamageType.Physical
        };

        _hitbox.Activate(info);
    }

    /// <summary>Turns the hitbox off.</summary>
    protected override void FinishAttack()
    {
        if (_hitbox != null)
            _hitbox.Deactivate();
    }
}
