using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles collision detection for an enemy's melee attack.
/// While a swing is active, checks the box every physics step and damages the enemy's target once per swing.
///
/// CHANGED: no longer uses OnTriggerEnter. A CharacterController only sends trigger events while it moves,
/// so a player standing still inside the box was never hit. The BoxCollider sets the shape and is
/// turned on during a swing so it's visible in the Scene view.
/// </summary>

[RequireComponent(typeof(BoxCollider))]
public class EnemyMeleeHitbox : MonoBehaviour
{
    [SerializeField] private EnemyBase _owner;
    [SerializeField] private BoxCollider _trigger;

    [Tooltip("Layers the swing can hit. Set to the Player layer to skip the floor, walls and other enemies.")]
    [SerializeField] private LayerMask _hitLayers = ~0; 

    private DamageInfo _damageInfo;
    private readonly HashSet<IDamageable> _alreadyHit = new(); 
    private readonly Collider[] _overlaps = new Collider[16];  
    public bool IsActive { get; private set; }

    private void Awake()
    {
        // Gets the owning enemy if not already assigned
        if (_owner == null)
            _owner = GetComponentInParent<EnemyBase>();

        // Gets the hitbox's collider if not already assigned
        if (_trigger == null)
            _trigger = GetComponent<BoxCollider>();

        _trigger.isTrigger = true;
        IsActive = false;
        _trigger.enabled = false;
    }

    // Called by MeleeAttack when the wind-up finishes
    public void Activate(DamageInfo info)
    {
        // Save the damage info for this attack
        _damageInfo = info;

        // Clear previous hits so a new swing can hit again
        _alreadyHit.Clear();

        IsActive = true;
        _trigger.enabled = true;

        
        Physics.SyncTransforms();
        CheckOverlaps();
    }

    public void Deactivate()
    {
        // Stop registering damaging hits
        IsActive = false;

        _trigger.enabled = false;

        // Clear the list of targets hit
        _alreadyHit.Clear();
    }

   
    private void FixedUpdate()
    {
        if (IsActive)
            CheckOverlaps();
    }

    
    private void CheckOverlaps()
    {
        if (_owner == null || _owner.Target == null)
            return;

        IDamageable target = FindTargetDamageable();
        if (target == null || _alreadyHit.Contains(target))
            return;

        Vector3 center = _trigger.transform.TransformPoint(_trigger.center);
        Vector3 scale = _trigger.transform.lossyScale;
        Vector3 halfExtents = Vector3.Scale(
            _trigger.size * 0.5f,
            new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));

        int count = Physics.OverlapBoxNonAlloc(
            center, halfExtents, _overlaps, _trigger.transform.rotation, _hitLayers, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Collider other = _overlaps[i];

            // Ignore our own hitbox and the enemy's own colliders.
            if (other == _trigger || other.transform.IsChildOf(_owner.transform))
                continue;

            // CHANGED: matches by IDamageable instead of by transform, so any of the target's colliders counts,
            // whichever object of the player it's on.
            if (other.GetComponentInParent<IDamageable>() != target)
                continue;

            _alreadyHit.Add(target);
            target.TakeDamage(_damageInfo);
            return;
        }
    }

  
    private IDamageable FindTargetDamageable()
    {
        Transform target = _owner.Target;
        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        return damageable ?? target.GetComponentInChildren<IDamageable>();
    }

    // CHANGED: new. Shows the hitbox, red while a swing is active.
    private void OnDrawGizmosSelected()
    {
        BoxCollider box = _trigger != null ? _trigger : GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.color = IsActive ? Color.red : new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.matrix = box.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}