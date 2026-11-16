using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Moves the impact dot to where the next shot lands, using Shoot.LastAim. The position comes from AimSolution.AimPoint,
/// which CrossHairAim finds with a ray from the camera through the screen center. It never uses a ray from the muzzle,
/// so the dot doesn't move with the gun model or its animations. While relaxed, the dot stays at the screen center.
/// </summary>
// INTEGRATION: new. Replaces the dot code in PlayerAimController. Reads Shoot.LastAim and Shoot.HasAim (both new).
public class CrosshairDot : MonoBehaviour
{
    [SerializeField] private Shoot _shoot;
    [SerializeField] private Camera _camera;
    [SerializeField] private RectTransform _dot;
    [SerializeField] private PlayerCombatState _combatState;

    private Graphic _dotGraphic; // The dot's Image or other Graphic, turned off to hide it.

    /// <summary>Finds any references left empty.</summary>
    private void Awake()
    {
        if (!_dot) _dot = transform as RectTransform;
        if (_dot) _dotGraphic = _dot.GetComponent<Graphic>();
        if (!_camera) _camera = Camera.main;
        if (!_shoot) _shoot = FindFirstObjectByType<Shoot>();
        if (!_combatState) _combatState = FindFirstObjectByType<PlayerCombatState>();
    }

    /// <summary>
    /// Places the dot. Runs in LateUpdate, after Shoot.Update has worked out this frame's aim.
    /// </summary>
    private void LateUpdate()
    {
        if (!_dot) return;

        Vector2 screenCentre = new Vector2(Screen.width / 2f, Screen.height / 2f);

        if (!_shoot || !_camera || !_shoot.HasAim ||
            (_combatState && _combatState.CurrentCombatState == PlayerCombatStates.Relaxed))
        {
            SetVisible(true);
            _dot.position = screenCentre;
            return;
        }

        Vector3 screenPosition = _camera.WorldToScreenPoint(_shoot.LastAim.AimPoint);

        // Hide the dot if the aim point is behind the camera.
        bool isVisible = screenPosition.z > 0f;
        SetVisible(isVisible);

        if (isVisible)
            _dot.position = screenPosition;
    }

    /// <summary>Shows or hides the dot.</summary>
    private void SetVisible(bool visible)
    {
        if (_dotGraphic) _dotGraphic.enabled = visible;
    }
}