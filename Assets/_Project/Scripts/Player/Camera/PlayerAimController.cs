using UnityEngine;

/// <summary>
/// Works out the shot direction from the muzzle toward the crosshair, and moves the impact dot to where it lands.
/// </summary>
// INTEGRATION: no longer used. Shoot now aims with CrossHairAim, and CrosshairDot draws the dot. Kept so scenes that
// still have this component don't lose their settings; safe to remove from the player prefab and delete.
public class PlayerAimController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _camera;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private PlayerCameraController _cameraController;
    [SerializeField] private RectTransform _impactDot;
    [SerializeField] private PlayerCombatState _combatState;

    [Header("Settings")]
    [SerializeField] private float _maxDistance = 100f; // How far the aim and impact rays reach.
    [SerializeField, Range(0f, 90f)] private float _maxAimAngle = 20f; // Most the shot may turn away from the barrel.
    [SerializeField] private LayerMask _hitMask;

    /// <summary>Where a shot from the muzzle lands.</summary>
    public Vector3 ImpactPoint { get; private set; }

    /// <summary>The point under the crosshair.</summary>
    public Vector3 AimPoint { get; private set; }

    /// <summary>The shot's direction from the muzzle.</summary>
    public Vector3 ShotDirection { get; private set; }

    /// <summary>Finds the combat state if empty.</summary>
    private void Awake()
    {
        if (!_combatState)
            _combatState = FindFirstObjectByType<PlayerCombatState>();
    }

    /// <summary>Works out the aim and places the dot: on the impact point in combat, at the screen center when relaxed.</summary>
    private void LateUpdate()
    {
        CalculateAim();
        CalculateImpactPoint();
        
        if (_combatState.CurrentCombatState != PlayerCombatStates.Relaxed)
        {
            UpdateCrosshair();
        }
        else
        {
            _impactDot.position = new Vector2(Screen.width / 2f, Screen.height / 2f);
        }
    }

    /// <summary>
    /// Finds the camera's target and calculates the projectile direction from the muzzle.
    /// Limits the shot angle relative to the barrel to prevent extreme deviations.
    /// </summary>
    private void CalculateAim()
    {
        // Find camera's target
        Ray cameraRay = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        if (Physics.Raycast(cameraRay,
                out RaycastHit hit,
                _maxDistance,
                _hitMask))
        {
            AimPoint = hit.point;
        }
        else
        {
            AimPoint = cameraRay.GetPoint(_maxDistance);
        }

        // Direction needed to hit that target
        Vector3 desiredDirection = (AimPoint - _muzzle.position).normalized;

        // Limit deviation from the barrel
        ShotDirection = Vector3.RotateTowards(
            _muzzle.forward,
            desiredDirection,
            _maxAimAngle * Mathf.Deg2Rad,
            0f
        ).normalized;
    }

    /// <summary>
    /// Predicts the projectile's impact point using a raycast from the muzzle
    /// along the calculated shot direction.
    /// </summary>
    private void CalculateImpactPoint()
    {
        Ray ray = new Ray(_muzzle.position, ShotDirection);

        if (Physics.Raycast(ray, out RaycastHit hit, _maxDistance, _hitMask))
        {
            ImpactPoint = hit.point;
        }
        else
        {
            ImpactPoint = ray.GetPoint(_maxDistance);
        }
    }

    /// <summary>
    /// Positions the impact indicator on screen and hides it if the point is behind the camera.
    /// </summary>
    private void UpdateCrosshair()
    {
        Vector3 screenPosition = _camera.WorldToScreenPoint(ImpactPoint);

        bool isVisible = screenPosition.z > 0f;

        _impactDot.gameObject.SetActive(isVisible);

        if (isVisible)
            _impactDot.position = screenPosition;
    }
}