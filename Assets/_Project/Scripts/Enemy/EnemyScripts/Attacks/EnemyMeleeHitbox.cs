using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles collision detection for an enemy's melee attack.
/// The hitbox only registers hits while an attack is active.
/// </summary>

[RequireComponent(typeof(BoxCollider))]
public class EnemyMeleeHitbox : MonoBehaviour
{
    [SerializeField] private EnemyBase _owner;
    [SerializeField] private BoxCollider _trigger;
    private DamageInfo _damageInfo;
    private HashSet<Collider> _alreadyHit = new();
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

        BoxCollider box = _trigger;

        if (box == null)
        {
            Debug.LogError("EnemyMeleeHitbox requires a BoxCollider.");
            Deactivate();
            return;
        }

        Physics.SyncTransforms();

        Vector3 center = box.transform.TransformPoint(box.center);
        Vector3 scale = box.transform.lossyScale;
        Vector3 halfExtents = Vector3.Scale(
            box.size * 0.5f,
            new Vector3(
                Mathf.Abs(scale.x),
                Mathf.Abs(scale.y),
                Mathf.Abs(scale.z)
            )
            );
        
        Collider[] overlaps = Physics.OverlapBox(center, halfExtents, box.transform.rotation, ~0, QueryTriggerInteraction.Collide);

        foreach (Collider other in overlaps)
        {

            // Ignore our own hitbox.
            if (other == _trigger)
                continue;

            // Reuse our existing damage logic.
            OnTriggerEnter(other);
        }
    }

    public void Deactivate()
    {
        // Stop registering damaging hits
        IsActive = false;

        _trigger.enabled = false;

        // Clear the list of colliders hit
        _alreadyHit.Clear();
        
    }
    // Unity calls this when another collider enters the hitbox
    private void OnTriggerEnter(Collider other)
    {
        IDamageable damageable = other.GetComponentInParent<IDamageable>();

        // Only register hits during the melee attack
        if (!IsActive || _owner == null || _owner.Target == null)
            return;
        
        // Ignore antything that isn't the enemy's target
        if(other.transform != _owner.Target && !other.transform.IsChildOf(_owner.Target))
            return;
        
        // Ignore objects that cannot receive damage.
        if (damageable == null)
            return;

        if (!_alreadyHit.Add(other))
                return;

        damageable.TakeDamage(_damageInfo);
    }
}