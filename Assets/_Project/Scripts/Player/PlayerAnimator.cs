using System;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("Tuning")]
    [SerializeField] private float _dampTime = 0.1f;
    [SerializeField] private float _idleAnimationMultiplier = 0.7f;
    [SerializeField] private float _sprintAnimationMultiplier = 1.4f;

    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private PlayerInput _input;
    [SerializeField] private PlayerCameraController _cameraController;
    [SerializeField] private CameraTargetController _cameraTargetController;
    [SerializeField] private Dash _playerDash;
    [SerializeField] private Shoot _shoot;

    // Animator parameter names
    private static readonly int IsSprinting = Animator.StringToHash("IsSprinting");
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int LocomotionSpeed = Animator.StringToHash("LocomotionSpeed");
    private static readonly int Jump = Animator.StringToHash("Jump");
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int IsRising = Animator.StringToHash("IsRising");
    private static readonly int IsFalling = Animator.StringToHash("IsFalling");
    private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int IsAiming = Animator.StringToHash("IsAiming");
    private static readonly int AimPitch = Animator.StringToHash("AimPitch");
    private static readonly int Dash = Animator.StringToHash("Dash");
    private static readonly int Fire = Animator.StringToHash("Fire");

    private void OnEnable()
    {
        _playerMovement.OnJump += JumpAnimation;
        _playerDash.OnDashed += DashAnimation;
        _cameraController.OnAimChanged += AimingAnimation;
        _shoot.OnFire += FireAnimation;
    }

    private void OnDisable()
    {
        _playerMovement.OnJump -= JumpAnimation;
        _playerDash.OnDashed -= DashAnimation;
        _cameraController.OnAimChanged -= AimingAnimation;
        _shoot.OnFire -= FireAnimation;
    }

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
        if (_shoot)
            _shoot = FindFirstObjectByType<Shoot>();
    }

    /// <summary>
    /// Updates locomotion, airborne, and aim animation parameters each frame.
    /// Uses damped movement input for smoother blend tree transitions.
    /// </summary>
    private void Update()
    {
        if (!_animator || !_input)
            return;

        Vector2 moveInput = _input.Move;

        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        float speedMultiplier;
        if (_playerMovement.IsSprinting && isMoving)
            speedMultiplier = _sprintAnimationMultiplier;
        else if (!isMoving)
            speedMultiplier = _idleAnimationMultiplier;
        else
            speedMultiplier = 1f;

        _animator.SetFloat(LocomotionSpeed, speedMultiplier);
        _animator.SetBool(IsSprinting, isMoving && _input.IsSprinting);
        _animator.SetBool(IsMoving, isMoving);
        _animator.SetFloat(MoveX, moveInput.x, _dampTime, Time.deltaTime);
        _animator.SetFloat(MoveY, moveInput.y, _dampTime, Time.deltaTime);
        _animator.SetBool(IsRising, _playerMovement.IsRising);
        _animator.SetBool(IsFalling, _playerMovement.IsFalling);
        _animator.SetBool(IsGrounded, _playerMovement.IsGrounded);
        _animator.SetFloat(AimPitch, _cameraTargetController.AimPitch);
    }

    private void JumpAnimation() => _animator.SetTrigger(Jump);
    private void AimingAnimation(bool aiming) => _animator.SetBool(IsAiming, aiming);
    private void DashAnimation() => _animator.SetTrigger(Dash);
    private void FireAnimation() => _animator.SetTrigger(Fire);
}