using System;
using UnityEngine;

/// <summary>
/// Fires the player's gun. Each frame, works out the aim with CrossHairAim.Solve, stores it in LastAim, and passes it
/// to Gun.TryFire. The gun's fire mode decides whether a shot happens that frame.
/// </summary>
public class Shoot : MonoBehaviour
{
    [Tooltip("Found in children if empty.")]
    [SerializeField] private Gun _gun;
    [Tooltip("Uses InputManager.Instance if empty.")]
    [SerializeField] private InputManager _input;
    [Tooltip("Uses Camera.main if empty. The crosshair is the center of this camera's view.")]
    [SerializeField] private Camera _camera;
    [Tooltip("Layers the aim ray and the shot collide with.")]
    [SerializeField] private LayerMask _hitMask;
    [Tooltip("Largest angle between the muzzle's path and the crosshair ray before the shot starts on the crosshair ray instead of the muzzle.")]
    [SerializeField, Range(1f, 90f)] private float _maxMuzzleAngle = 90f;

    private GameObject _owner; // The player root. Its colliders never block the aim.

    /// <summary>Fires each time the gun shoots, including every shot of automatic fire. PlayerCombatState and PlayerAnimator listen.</summary>
    public event Action OnFired;

    /// <summary>This frame's aim. SecondaryFireAbility fires along its direction, and GunAimer points the barrel at its aim point.</summary>
    public AimSolution LastAim { get; private set; }

    /// <summary>False until the first aim is worked out, so readers don't use an empty LastAim.</summary>
    public bool HasAim { get; private set; }

    private void Awake()
    {
        if (!_input)
            _input = InputManager.Instance;
        if (!_gun)
            _gun = GetComponentInChildren<Gun>();
        if (!_gun)
            Debug.LogWarning("No Gun found for " + gameObject.name);
        if (!_camera)
            _camera = Camera.main;

        PlayerRoot root = GetComponentInParent<PlayerRoot>();
        _owner = root != null ? root.gameObject : gameObject;
    }

    private void OnEnable()
    {
        _input.OnFirePressed += PullTrigger;
    }

    private void OnDisable()
    {
        _input.OnFirePressed -= PullTrigger;

        // Let go of the trigger, so the gun doesn't keep firing.
        if (_gun) _gun.ReleaseTrigger();
    }

    private void Update()
    {
        if (!_gun || !_gun.Muzzle || !_camera) return;

        LastAim = CrossHairAim.Solve(_camera, _gun.Muzzle.position, _gun.Range, _hitMask, _owner, _gun.BulletRadius, _maxMuzzleAngle);
        HasAim = true;

        // The press is caught by PullTrigger. Releasing is checked here, because InputManager has no released event for Fire.
        if (!_input.IsFiring) _gun.ReleaseTrigger();

        // The gun's fire mode, from its barrel, decides whether this frame shoots.
        if (_gun.TryFire(LastAim))
        {
            SoundController.Instance.PlaySFX("Gun Shot", _gun.Muzzle.position);
            OnFired?.Invoke();
        }
    }

    private void PullTrigger()
    {
        if (_gun) _gun.PullTrigger();
    }
}
