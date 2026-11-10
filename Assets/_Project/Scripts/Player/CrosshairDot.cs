using UnityEngine;
using UnityEngine.UI;

// Moves the impact dot to where the next shot lands, using Shoot.LastAim. Replaces the dot code in PlayerAimController.
// The position comes from AimSolution.AimPoint, which CrossHairAim finds with a ray from the camera through the screen centre.
// It never uses a ray from the muzzle, so the dot doesn't move with the gun model or its animations.
public class CrosshairDot : MonoBehaviour
{
    [SerializeField] private Shoot _shoot;
    [SerializeField] private Camera _camera;
    [SerializeField] private RectTransform _dot;
    [SerializeField] private PlayerCombatState _combatState;

    private Graphic _dotGraphic; 

    private void Awake()
    {
        if (!_dot) _dot = transform as RectTransform;
        if (_dot) _dotGraphic = _dot.GetComponent<Graphic>();
        if (!_camera) _camera = Camera.main;
        if (!_shoot) _shoot = FindFirstObjectByType<Shoot>();
        if (!_combatState) _combatState = FindFirstObjectByType<PlayerCombatState>();
    }

   
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

    private void SetVisible(bool visible)
    {
        if (_dotGraphic) _dotGraphic.enabled = visible;
    }
}