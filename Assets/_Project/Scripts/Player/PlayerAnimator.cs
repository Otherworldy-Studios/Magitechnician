using System;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Drives the player's Animator from movement, dash, aim, and combat state. Sets blend values every frame, and
/// triggers for jumps, dashes, and shots when their events fire.
/// </summary>
public class PlayerAnimator : MonoBehaviour
{
    [Header("Tuning")]
    [SerializeField] private float _dampTime = 0.1f; // Seconds MoveX and MoveY take to catch up with the input.
    [SerializeField] private float _idleAnimationMultiplier = 0.7f;   // Not read by this script yet.
    [SerializeField] private float _sprintAnimationMultiplier = 1.4f; // Not read by this script yet.

    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private PlayerMovement _playerMovement;
    // INTEGRATION: removed the PlayerInput field. Move input now comes from InputManager in Update.
    [SerializeField] private PlayerCameraController _cameraController;
    [SerializeField] private CameraTargetController _cameraTargetController;
    [SerializeField] private Dash _playerDash;
    [SerializeField] private Shoot _shoot;
    [SerializeField] private PlayerCombatState _combatState;

    // Animator parameter IDs. Static readonly, so they're named like constants.
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int DashX = Animator.StringToHash("DashX");
    private static readonly int DashY = Animator.StringToHash("DashY");
    private static readonly int DashTrigger = Animator.StringToHash("Dash"); // Same parameter as Dash below.
    private static readonly int Jump = Animator.StringToHash("Jump");
    private static readonly int IsRising = Animator.StringToHash("IsRising");
    private static readonly int IsFalling = Animator.StringToHash("IsFalling");
    private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int IsAiming = Animator.StringToHash("IsAiming");
    private static readonly int AimPitch = Animator.StringToHash("AimPitch");
    private static readonly int Dash = Animator.StringToHash("Dash");
    private static readonly int Fire = Animator.StringToHash("Fire");
    private static readonly int CombatState = Animator.StringToHash("CombatState");

    /// <summary>Listens for jumps, dashes, aim changes, and shots.</summary>
    private void OnEnable()
    {
        _playerMovement.OnJumped += JumpAnimation;
        _playerDash.OnDashed += HandleDashAnimation;
        _cameraController.OnAimChanged += AimingAnimation;
        _shoot.OnFired += FireAnimation; // INTEGRATION: Shoot now fires this on every shot the Gun makes.
    }

    /// <summary>Stops listening.</summary>
    private void OnDisable()
    {
        _playerMovement.OnJumped -= JumpAnimation;
        _playerDash.OnDashed -= HandleDashAnimation;
        _cameraController.OnAimChanged -= AimingAnimation;
        _shoot.OnFired -= FireAnimation;
    }

    /// <summary>Finds any references left empty.</summary>
    private void Awake()
    {
        if (!_animator)
            _animator = GetComponentInChildren<Animator>();
        if (!_playerMovement)
            _playerMovement = GetComponent<PlayerMovement>();
        if (!_cameraController)
            _cameraController = FindFirstObjectByType<PlayerCameraController>();
        if (!_cameraTargetController)
            _cameraTargetController = FindFirstObjectByType<CameraTargetController>();
        if (!_playerDash)
            _playerDash = FindFirstObjectByType<Dash>();
        if (!_shoot)
            _shoot = FindFirstObjectByType<Shoot>();
        if (!_combatState)
            _combatState = FindFirstObjectByType<PlayerCombatState>();
    }

    /// <summary>
    /// Updates locomotion, airborne, and aim animation parameters each frame.
    /// Uses damped movement input for smoother blend tree transitions.
    /// </summary>
    private void Update()
    {
        if (!_animator)
            return;

        Vector2 moveInput = InputManager.Instance.Move;

        _animator.SetFloat(MoveX, moveInput.x, _dampTime, Time.deltaTime);
        _animator.SetFloat(MoveY, moveInput.y, _dampTime, Time.deltaTime);
        _animator.SetBool(IsRising, _playerMovement.IsRising);
        _animator.SetBool(IsFalling, _playerMovement.IsFalling);
        _animator.SetBool(IsGrounded, _playerMovement.IsGrounded);
        _animator.SetFloat(AimPitch, _cameraTargetController.AimPitch);
        _animator.SetInteger(CombatState, (int)_combatState.CurrentCombatState);
    }
    
    /// <summary>Sets the dash direction relative to the player and triggers the dash animation. Runs on Dash.OnDashed.</summary>
    private void HandleDashAnimation()
    {
        Vector3 localDirection = transform.InverseTransformDirection(_playerDash.DashDirection);

        _animator.SetFloat(DashX, localDirection.x);
        _animator.SetFloat(DashY, localDirection.z);

        _animator.SetTrigger(DashTrigger);
    }

    /// <summary>Triggers the jump animation. Runs on PlayerMovement.OnJumped.</summary>
    private void JumpAnimation() => _animator.SetTrigger(Jump);
    /// <summary>Sets the aiming flag. Runs on PlayerCameraController.OnAimChanged.</summary>
    private void AimingAnimation(bool aiming) => _animator.SetBool(IsAiming, aiming);
    /// <summary>Triggers the fire animation. Runs on Shoot.OnFired.</summary>
    private void FireAnimation() => _animator.SetTrigger(Fire);
}