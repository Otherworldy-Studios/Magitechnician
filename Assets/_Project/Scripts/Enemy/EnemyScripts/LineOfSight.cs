using UnityEngine;

/// <summary>
/// Determines whether an enemy can see its target.
///
/// The target has to be inside a horizontal cone in front of the enemy, and nothing on a blocking layer
/// can be between the eyes and the target. Once the enemy is locked on, Track Once Spotted skips the
/// cone check, so the enemy keeps tracking the target while turning.
///
/// The detection range comes from EnemyBase.DetectionRange. The gizmo shows the cone at that range,
/// and in Play mode a line to the target: green when it can see the target, red when it can't.
/// </summary>
// INTEGRATION: the cone, the target point lookup, and the gizmo are new. The raycast in Check is the original
// HasLineOfSight. EnemyBase.Update calls HasLineOfSight every frame to decide whether to lock on.
[RequireComponent(typeof(EnemyBase))]
public class LineOfSight : MonoBehaviour, IEnemyLineOfSight
{
    [Tooltip("Where the enemy sees from. Uses the enemy's position if empty.")]
    [SerializeField] private Transform _eyes;

    [Tooltip("Layers that block sight, such as walls.")]
    [SerializeField] private LayerMask _blockingLayers = ~0;

    [Header("Cone")]
    [Tooltip("Full width of the view cone in degrees. 360 sees in every direction.")]
    [SerializeField, Range(1f, 360f)] private float _viewAngle = 120f;

    // INTEGRATION: new. Reads EnemyBase.HasTarget, which is the lock-on from the frame before.
    [Tooltip("Once locked on, skip the cone check so the enemy keeps tracking the target. Walls still break sight.")]
    [SerializeField] private bool _trackOnceSpotted = true;

    // INTEGRATION: new. Before, a target without a CharacterController was never seen.
    [Tooltip("Used when the target has no CharacterController. Aims this far above the target's position.")]
    [SerializeField] private float _fallbackTargetHeight = 1f;

    [Header("Gizmo")]
    [SerializeField] private Color _coneColor = new(1f, 0.85f, 0f, 1f);
    [SerializeField, Range(4, 48)] private int _arcSegments = 24; // How many lines draw the cone's curved edge.

    private EnemyBase _enemy;

    // INTEGRATION: new. Cached, so the GetComponentInChildren search in GetTargetPoint only runs when the target changes.
    private Transform _cachedTarget;
    private CharacterController _targetController;

    private bool _lastResult; // INTEGRATION: new. The last HasLineOfSight result, for the gizmo color.

    /// <summary>Finds the EnemyBase.</summary>
    private void Awake()
    {
        _enemy = GetComponent<EnemyBase>();
    }

    /// <summary>True if the target is in the cone and nothing on a blocking layer is in the way.</summary>
    // INTEGRATION: the original body moved to Check, so the result can be stored for the gizmo.
    public bool HasLineOfSight()
    {
        _lastResult = Check();
        return _lastResult;
    }

    /// <summary>
    /// Checks the cone, then casts a ray from the eyes to the target. The target is visible when the closest hit that
    /// is either the target or on a blocking layer is the target.
    /// </summary>
    private bool Check()
    {
        if (_enemy.Target == null)
            return false;

        // INTEGRATION: was _eyes.position, which threw when no eyes were assigned.
        Vector3 origin = EyePosition;

        // INTEGRATION: was GetComponent<CharacterController> on the target. EnemyBase now targets the
        // CharacterController's transform, and GetTargetPoint also searches children and has a fallback.
        Vector3 destination = GetTargetPoint();

        if (!InsideCone(destination))
            return false;

        Vector3 direction = destination - origin;
        float distance = direction.magnitude;

        if (distance <= 0f)
            return true;

        // Check all hits along the ray.
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction.normalized,
            distance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = Mathf.Infinity;
        Transform closestObject = null;

        foreach (RaycastHit hit in hits)
        {
            // Ignore the enemy's own colliders.
            if (hit.transform.IsChildOf(transform))
                continue;

            bool isPlayer = hit.transform.IsChildOf(_enemy.Target);

            bool isBlockingLayer = (_blockingLayers.value & (1 << hit.collider.gameObject.layer)) != 0;

            // Ignore objects that don't block sight, but keep the player.
            if (!isPlayer && !isBlockingLayer)
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestObject = hit.transform;
            }
        }

        return closestObject != null && closestObject.IsChildOf(_enemy.Target);
    }

    /// <summary>
    /// True if a point is inside the view cone. Compares directions on the ground plane only, so height differences
    /// don't push the player out of the cone.
    /// </summary>
    private bool InsideCone(Vector3 point)
    {
        if (_viewAngle >= 360f)
            return true;

        // Already locked on: keep tracking while the enemy turns.
        if (_trackOnceSpotted && _enemy.HasTarget)
            return true;

        Vector3 toPoint = point - transform.position;
        toPoint.y = 0f;

        if (toPoint.sqrMagnitude < 0.0001f)
            return true;

        return Vector3.Angle(Forward, toPoint) <= _viewAngle * 0.5f;
    }

    /// <summary>Where the enemy sees from: _eyes, or the enemy's position.</summary>
    private Vector3 EyePosition => _eyes != null ? _eyes.position : transform.position;

    /// <summary>The enemy root's forward on the ground plane. The NavMeshAgent turns the root, so the cone turns with it.</summary>
    private Vector3 Forward
    {
        get
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }
    }

    /// <summary>
    /// The center of the target's CharacterController, searched in its children, or the target's position plus
    /// _fallbackTargetHeight.
    /// </summary>
    private Vector3 GetTargetPoint()
    {
        if (_cachedTarget != _enemy.Target)
        {
            _cachedTarget = _enemy.Target;
            _targetController = _cachedTarget.GetComponentInChildren<CharacterController>();
        }

        if (_targetController != null)
            return _targetController.bounds.center;

        return _enemy.Target.position + Vector3.up * _fallbackTargetHeight;
    }

    /// <summary>
    /// Draws the view cone at the detection range, and in Play mode a line to the target that is green when the
    /// target is visible and red when it isn't.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // In Edit mode Awake hasn't run, so the EnemyBase is looked up here.
        EnemyBase owner = _enemy != null ? _enemy : GetComponent<EnemyBase>();
        float range = owner != null ? owner.DetectionRange : 10f;
        Vector3 origin = EyePosition;
        Vector3 forward = Forward;

        Gizmos.color = _coneColor;

        float half = Mathf.Min(_viewAngle, 360f) * 0.5f;
        Vector3 previous = origin + Quaternion.Euler(0f, -half, 0f) * forward * range;

        // Edge lines, unless the cone is a full circle.
        if (_viewAngle < 360f)
        {
            Gizmos.DrawLine(origin, previous);
            Gizmos.DrawLine(origin, origin + Quaternion.Euler(0f, half, 0f) * forward * range);
        }

        // Arc at the detection range.
        for (int i = 1; i <= _arcSegments; i++)
        {
            float angle = Mathf.Lerp(-half, half, i / (float)_arcSegments);
            Vector3 next = origin + Quaternion.Euler(0f, angle, 0f) * forward * range;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }

        // Center line.
        Gizmos.DrawLine(origin, origin + forward * Mathf.Min(1.5f, range));

        if (Application.isPlaying && owner != null && owner.Target != null)
        {
            Gizmos.color = _lastResult ? Color.green : Color.red;
            Gizmos.DrawLine(origin, GetTargetPoint());
        }
    }
}
