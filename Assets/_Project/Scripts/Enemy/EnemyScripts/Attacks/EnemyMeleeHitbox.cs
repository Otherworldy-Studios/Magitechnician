using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Finds hits for an enemy's melee attack. While a swing is active, checks the box every physics step and damages
/// the enemy's target once per swing. The BoxCollider sets the shape, and is turned on during a swing so it shows in
/// the Scene view.
/// </summary>
// INTEGRATION: no longer uses OnTriggerEnter. A CharacterController only sends trigger events while it moves, so a
// player standing still inside the box was never hit, and damage only landed when the player walked out. MeleeAttack
// still calls Activate and Deactivate as before.
[RequireComponent(typeof(BoxCollider))]
public class EnemyMeleeHitbox : MonoBehaviour
{
    [SerializeField] private EnemyBase _owner;      // Found in parents if empty.
    [SerializeField] private BoxCollider _trigger;  // The box's shape. Found on this object if empty.

    // INTEGRATION: new. Used by the overlap check that replaced OnTriggerEnter.
    [Tooltip("Layers the swing can hit. Set to the Player layer to skip the floor, walls and other enemies.")]
    [SerializeField] private LayerMask _hitLayers = ~0;

    private DamageInfo _damageInfo; // The damage for the current swing, from MeleeAttack.

    // Targets already damaged this swing, so each is hit only once.
    // INTEGRATION: holds IDamageable instead of Transform, to match the new target check in CheckOverlaps.
    private readonly HashSet<IDamageable> _alreadyHit = new();

    private readonly Collider[] _overlaps = new Collider[16]; // INTEGRATION: new. Buffer for OverlapBoxNonAlloc.

    /// <summary>True during a swing.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Finds the owner and the box, and starts with the box off.</summary>
    private void Awake()
    {
        if (_owner == null)
            _owner = GetComponentInParent<EnemyBase>();

        if (_trigger == null)
            _trigger = GetComponent<BoxCollider>();

        _trigger.isTrigger = true;
        IsActive = false;
        _trigger.enabled = false;
    }

    /// <summary>Starts a swing with the given damage. MeleeAttack calls this when the wind-up finishes.</summary>
    public void Activate(DamageInfo info)
    {
        _damageInfo = info;

        // Clear previous hits so a new swing can hit again.
        _alreadyHit.Clear();

        IsActive = true;
        _trigger.enabled = true;

        // INTEGRATION: new. Checks right away, so a player already in the box is hit this frame. SyncTransforms makes
        // the overlap use positions moved since the last physics step.
        Physics.SyncTransforms();
        CheckOverlaps();
    }

    /// <summary>Ends the swing. MeleeAttack calls this when the active time ends or the attack is canceled.</summary>
    public void Deactivate()
    {
        IsActive = false;

        _trigger.enabled = false;

        _alreadyHit.Clear();
    }

    /// <summary>Checks for hits every physics step during a swing.</summary>
    // INTEGRATION: new. Replaces OnTriggerEnter.
    private void FixedUpdate()
    {
        if (IsActive)
            CheckOverlaps();
    }

    /// <summary>
    /// Damages the owner's target if one of its colliders is inside the box and it hasn't been hit this swing.
    /// </summary>
    // INTEGRATION: new. Replaces OnTriggerEnter.
    private void CheckOverlaps()
    {
        if (_owner == null || _owner.Target == null)
            return;

        IDamageable target = FindTargetDamageable();
        if (target == null || _alreadyHit.Contains(target))
            return;

        // The box in world space, including the object's scale.
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

            // Ignore this hitbox and the enemy's own colliders.
            if (other == _trigger || other.transform.IsChildOf(_owner.transform))
                continue;

            // INTEGRATION: matches by IDamageable instead of by transform, so any of the target's colliders counts,
            // whichever object of the player it's on. EnemyBase targets the CharacterController, while Health is on
            // the player root.
            if (other.GetComponentInParent<IDamageable>() != target)
                continue;

            _alreadyHit.Add(target);
            target.TakeDamage(_damageInfo);
            return;
        }
    }

    /// <summary>The IDamageable on the owner's target, searched in its parents, then its children.</summary>
    private IDamageable FindTargetDamageable()
    {
        Transform target = _owner.Target;
        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        return damageable ?? target.GetComponentInChildren<IDamageable>();
    }

    /// <summary>Draws the hitbox in the Scene view: red during a swing, orange otherwise.</summary>
    private void OnDrawGizmosSelected()
    {
        BoxCollider box = _trigger != null ? _trigger : GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.color = IsActive ? Color.red : new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.matrix = box.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
