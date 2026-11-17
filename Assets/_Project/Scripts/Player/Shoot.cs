using System;
using UnityEngine;

/// <summary>
/// Fires the player's gun. Each frame, works out the aim with CrossHairAim.Solve, stores it in LastAim, and passes it
/// to Gun.TryFire. The gun's fire mode decides whether a shot happens that frame.
/// </summary>
// INTEGRATION: rewritten. Before, it spawned a bullet prefab at a spawn point on each Fire press, aimed by
// PlayerAimController. Now the Gun (Team 3) spawns the projectiles and applies its stats and parts, and CrossHairAim
// replaces PlayerAimController. CrosshairDot (new) reads LastAim to draw the dot.
public class Shoot : MonoBehaviour
{
    // INTEGRATION: these replace _spawnPoint, _bulletPrefab, _aimController, and the PlayerInput reference.
    [Tooltip("Found in children if empty.")]
    [SerializeField] private Gun _gun;
    [Tooltip("Uses InputManager.Instance if empty.")]
    [SerializeField] private InputManager _input; // INTEGRATION: was PlayerInput. Reads Fire straight from InputManager.
    [Tooltip("Uses Camera.main if empty. The crosshair is the center of this camera's view.")]
    [SerializeField] private Camera _camera;
    [Tooltip("Layers the aim ray and the shot collide with.")]
    [SerializeField] private LayerMask _hitMask;
    [Tooltip("Largest angle between the shot and the crosshair ray before the shot starts from the camera instead of the muzzle.")]
    [SerializeField, Range(1f, 90f)] private float _maxMuzzleAngle = 90f;
    [SerializeField] private PlayerCameraController _cameraController; // Found in the scene if empty. Not read by this script.

    /// <summary>Fires each time the gun actually shoots. PlayerCombatState and PlayerAnimator listen.</summary>
    // INTEGRATION: now fires on each shot
    // the Gun makes, so automatic fire triggers it every shot, where before it fired once per press.
    public event Action OnFired;

    /// <summary>This frame's aim. CrosshairDot draws the dot at LastAim.AimPoint.</summary>
    public AimSolution LastAim { get; private set; }

    /// <summary>False until the first aim is worked out, so CrosshairDot doesn't draw at the world origin.</summary>
    public bool HasAim { get; private set; }

    private GameObject _owner; // INTEGRATION: new. The player root. Its colliders never block the aim.

    /// <summary>Finds any references left empty.</summary>
    private void Awake()
    {
        if (!_input)
            _input = InputManager.Instance;
        if (!_cameraController)
            _cameraController = FindFirstObjectByType<PlayerCameraController>();
        if (!_gun)
            _gun = GetComponentInChildren<Gun>();
        if (!_gun)
            Debug.LogWarning("No Gun found for " + gameObject.name);
        if (!_camera)
            _camera = Camera.main;

        PlayerRoot root = GetComponentInParent<PlayerRoot>();
        _owner = root != null ? root.gameObject : gameObject;
    }

    /// <summary>Listens for Fire presses.</summary>
    private void OnEnable()
    {
        // INTEGRATION: was PlayerInput.FirePressed calling Fire.
        _input.OnFirePressed += PullTrigger;
    }

    /// <summary>Stops listening and lets go of the trigger, so the gun doesn't keep firing.</summary>
    private void OnDisable()
    {
        _input.OnFirePressed -= PullTrigger;
        if (_gun) _gun.ReleaseTrigger();
    }

    /// <summary>Works out the aim, releases the trigger when Fire isn't held, and asks the gun to fire.</summary>
    // INTEGRATION: new. Replaces Fire, which spawned a bullet once per press.
    private void Update()
    {
        if (!_gun || !_gun.Muzzle || !_camera) return;

        // The aim starts from the camera's ray through the screen center, so the shot lands under the crosshair.
        // The muzzle is only used as the bullet's starting point, and only when it can reach that point.
        LastAim = CrossHairAim.Solve(
            _camera,
            _gun.Muzzle.position,
            _gun.Range,
            _hitMask,
            _owner,
            _gun.BulletRadius,
            _maxMuzzleAngle);
        HasAim = true;

        // The press is caught by PullTrigger. Releasing is checked here, because InputManager has no released event for Fire.
        if (!_input.IsFiring) _gun.ReleaseTrigger();

        // The gun's fire mode (from its barrel part) decides whether this frame shoots.
        if (_gun.TryFire(LastAim))
        {
            OnFired?.Invoke();
        }
    }

    /// <summary>Tells the gun the trigger was pressed. Runs on InputManager.OnFirePressed.</summary>
    private void PullTrigger() { if (_gun) _gun.PullTrigger(); }
}
