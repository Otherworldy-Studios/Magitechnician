using UnityEngine;

public class PlayerAimController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera _camera;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private PlayerCameraController _cameraController;
    [SerializeField] private RectTransform _impactDot;

    [Header("Settings")]
    [SerializeField] private float _maxDistance = 100f;
    [SerializeField, Range(0f, 90f)] private float _maxAimAngle = 20f;
    [SerializeField] private LayerMask _hitMask;

    public Vector3 ImpactPoint { get; private set; }

    public Vector3 AimPoint { get; private set; }
    public Vector3 ShotDirection { get; private set; }

    private void LateUpdate()
    {
        if (!_cameraController.IsAiming)
        {
            _impactDot.gameObject.SetActive(false);
            return;
        }

        CalculateAim();
        CalculateImpactPoint();
        UpdateCrosshair();
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