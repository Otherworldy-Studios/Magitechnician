using UnityEngine;

/// <summary>
/// Goes on the Dark Knight's shield collider. Stops projectiles that hit it, with no damage, and tells BlockReaction so
/// it can fire OnBulletBlocked for the spark and the clang. BlockReaction turns the collider on only while blocking.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BlockShield : MonoBehaviour, IProjectileBlocker
{
    [Tooltip("Found in parents if empty.")]
    [SerializeField] private BlockReaction _blockReaction;

    private void Awake()
    {
        if (_blockReaction == null)
            _blockReaction = GetComponentInParent<BlockReaction>();
    }

    public void BlockProjectile(Vector3 point, Vector3 direction)
    {
        if (_blockReaction != null)
            _blockReaction.ReportBulletBlocked(point);
    }
}
