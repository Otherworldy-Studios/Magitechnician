using UnityEngine;

/// <summary>
/// Controls the player's left hand using a custom two-bone IK solver.
/// Runs after GunAimer to ensure the hand follows the final weapon position.
///
/// To create a new grip point, add an empty GameObject to the weapon
/// or attachment and adjust its position and rotation to match the
/// desired hand pose.
///
/// Use SetPoint() to override the default grip point and ResetPoint()
/// to return to it. Use SetWeight() to smoothly enable or disable IK.
///
/// Does not require Unity's Animation Rigging package or Animator IK layers.
/// </summary>
[DefaultExecutionOrder(1000)]
public class LeftHandIK : MonoBehaviour
{
    [Header("Bones")]
    [SerializeField] private Transform _upperArm;
    [SerializeField] private Transform _forearm;
    [SerializeField] private Transform _hand;

    [Header("Targets")]
    [Tooltip("Default grip point used when no custom point is assigned. Position and rotation determine the desired hand pose.")]
    [SerializeField] private Transform _defaultPoint;

    [Tooltip("Determines the direction in which the elbow bends. Should be parented to the chest, not the arm.")]
    [SerializeField] private Transform _elbowHint;

    [Header("Settings")]
    [Tooltip("Initial IK weight. 0 uses the original animation, 1 fully applies IK.")]
    [SerializeField, Range(0f, 1f)] private float _weight = 1f;

    [Tooltip("Seconds required to transition between weights 0 and 1.")]
    [SerializeField, Min(0.01f)] private float _transitionDuration = 0.25f;

    private float _targetWeight;
    private Transform _currentPoint;

    private void Awake()
    {
        _targetWeight = _weight;
    }

    /// <summary>
    /// Overrides the default grip point with a custom target.
    /// The hand follows the target's position and rotation.
    /// Can be used for weapon attachments, alternative grips, or interactions.
    /// Passing null returns the hand to the default point.
    /// </summary>
    public void SetPoint(Transform point)
    {
        _currentPoint = point;
    }

    /// <summary>
    /// Removes the custom grip point and returns to the default point.
    /// Does not change the IK weight.
    /// </summary>
    public void ResetPoint()
    {
        _currentPoint = null;
    }

    /// <summary>
    /// Changes the fallback grip point, for example when equipping
    /// new attachments. Does not override an active custom grip point.
    /// </summary>
    public void SetDefaultPoint(Transform point)
    {
        _defaultPoint = point;
    }

    /// <summary>
    /// Sets the desired IK weight between 0 and 1.
    /// 0 preserves the original animation, while 1 fully applies IK.
    /// The weight transitions smoothly over time.
    /// Can safely be called every frame without restarting the transition.
    /// </summary>
    public void SetWeight(float weight)
    {
        _targetWeight = Mathf.Clamp01(weight);
    }

    /// <summary>
    /// Updates the IK weight and solves the arm after GunAimer
    /// has finished modifying the skeleton.
    /// </summary>
    private void LateUpdate()
    {
        _weight = Mathf.MoveTowards(
            _weight,
            _targetWeight,
            Time.deltaTime / _transitionDuration);

        Transform target = _currentPoint != null
            ? _currentPoint
            : _defaultPoint;

        if (target == null || _weight <= 0f)
            return;

        SolveIK(target);
    }

    /// <summary>
    /// Calculates the desired elbow and wrist positions using
    /// the arm's bone lengths, target position, and elbow hint.
    /// Rotates the bones to reach the target without stretching the arm,
    /// blending the result with the original pose based on IK weight.
    /// </summary>
    private void SolveIK(Transform target)
    {
        // Measure the current arm positions and bone lengths.
        Vector3 root = _upperArm.position;
        Vector3 elbow = _forearm.position;
        Vector3 wrist = _hand.position;

        float upperLength = Vector3.Distance(root, elbow);
        float lowerLength = Vector3.Distance(elbow, wrist);

        Vector3 toTarget = target.position - root;

        if (toTarget.sqrMagnitude < 0.000001f)
            return;

        Vector3 direction = toTarget.normalized;

        // Limit the target distance to the arm's physical reach.
        float distance = Mathf.Clamp(
            toTarget.magnitude,
            Mathf.Abs(upperLength - lowerLength) + 0.0001f,
            upperLength + lowerLength - 0.0001f);

        // Determine the elbow's bend direction using the hint.
        Vector3 bendDirection = Vector3.ProjectOnPlane(
            _elbowHint.position - root, direction);

        // Fall back to the current elbow direction if the hint
        // is aligned with the shoulder-to-target direction.
        if (bendDirection.sqrMagnitude < 0.000001f)
        {
            bendDirection = Vector3.ProjectOnPlane(
                elbow - root, direction);
        }

        if (bendDirection.sqrMagnitude < 0.000001f)
            return;

        bendDirection.Normalize();

        // Solve the arm triangle using the bone lengths
        // and the distance between the shoulder and target.
        float along =
            (upperLength * upperLength
            - lowerLength * lowerLength
            + distance * distance)
            / (2f * distance);

        float height = Mathf.Sqrt(Mathf.Max(
            0f, upperLength * upperLength - along * along));

        // Calculate the desired joint positions in world space.
        Vector3 desiredElbow =
            root + direction * along
            + bendDirection * height;

        Vector3 desiredWrist = root + direction * distance;

        // Rotate the upper arm toward the calculated elbow position.
        Quaternion upperCorrection = Quaternion.FromToRotation(
            _forearm.position - _upperArm.position,
            desiredElbow - _upperArm.position);

        _upperArm.rotation = Quaternion.Slerp(
            _upperArm.rotation,
            upperCorrection * _upperArm.rotation,
            _weight);

        // Rotate the forearm after the upper arm has been updated,
        // since its position depends on the upper arm's rotation.
        Quaternion lowerCorrection = Quaternion.FromToRotation(
            _hand.position - _forearm.position,
            desiredWrist - _forearm.position);

        _forearm.rotation = Quaternion.Slerp(
            _forearm.rotation,
            lowerCorrection * _forearm.rotation,
            _weight);

        // Match the hand's orientation to the target rotation.
        _hand.rotation = Quaternion.Slerp(
            _hand.rotation,
            target.rotation,
            _weight);
    }
}