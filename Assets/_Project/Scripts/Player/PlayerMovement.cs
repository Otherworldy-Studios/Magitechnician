using System;
using System.Timers;
using ScriptableSettings;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public enum MovementStates
    {
        Idle,
        Walking,
        Sprinting,
        Airborne,
        Dashing,
        Aiming
    }
    public MovementStates CurrentState { get; private set; }
    
    [SerializeField] private PlayerMovementSettings _settings;
    
    [Header("References")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private PlayerInput _input;
    [SerializeField] private PlayerCameraController _cameraController;
    [SerializeField] private Dash _dash;
    
    [Header("Public Variables")]
    public Vector3 Velocity => _velocity;
    public bool IsRising => _isRising;
    public bool IsFalling => _isFalling;
    public bool IsGrounded => _controller.isGrounded;
    public bool IsSprinting => CurrentState == MovementStates.Sprinting;
    public float MaxSpeed => _settings.SprintSpeed;
    
    public event Action OnJump;
    
    private CharacterController _controller;
    private Vector2 _moveInput;
    private Vector3 _velocity;
    private float _currentSpeed;
    private float _currentMaxSpeed = 0f;
    
    private bool _isRising;
    private bool _isFalling;
    
    private float _jumpBufferTimer;
    private float _coyoteTimeTimer;
    private bool _jumpHold;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        
        if(!_input)
            _input =  GetComponent<PlayerInput>();
        if(!_cameraController)
            _cameraController = FindFirstObjectByType<PlayerCameraController>();
        if(!_dash)
            _dash = GetComponent<Dash>();
    }

    private void Update()
    {
        _moveInput = _input.Move;
        
        AirState();
        HandleGravity();
        HandleJump();
        _dash.HandleDash(GetMovementDirection(), IsGrounded);
        StateController();
        ApplyMovement();
    }
    
    private void OnEnable()
    {
        _input.JumpPressed += JumpPressed;
        _input.JumpReleased += JumpReleased;
        _input.DashPressed += IntendToDash;
    }

    private void OnDisable()
    {
        _input.JumpPressed -= JumpPressed;
        _input.JumpReleased -= JumpReleased;
        _input.DashPressed -= IntendToDash;
    }

    private Vector3 GetMovementDirection()
    {
        Vector3 forward = _cameraTransform.forward;
        Vector3 right = _cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 direction = forward * _moveInput.y + right * _moveInput.x;

        direction = Vector3.ClampMagnitude(direction, 1f);

        if (direction.sqrMagnitude < 0.01f)
            return Vector3.zero;
        
        return direction;
    }

    private void ApplyMovement()
    {
        Vector3 horizontalVelocity;

        if (_dash.IsDashing)
        {
            horizontalVelocity = _dash.GetDashVelocity();
        }
        else
        {
            horizontalVelocity = GetMovementDirection() * SpeedControl();
        }

        Vector3 totalVelocity = horizontalVelocity + _velocity;

        _controller.Move(totalVelocity * Time.deltaTime);
    }

    private void HandleGravity()
    {
        if (_controller.isGrounded && _velocity.y < 0f)
            _velocity.y = _settings.GroundGravity;

        float gravity = _settings.Gravity;

        if (_isRising)
        {
            if (_jumpHold)
            {
                gravity *= _settings.MinJumpHeight / _settings.MaxJumpHeight;
            }
        }

        _velocity.y += gravity * Time.deltaTime;
    }

    private void StateController()
    {
        if (_dash.IsDashing)
            CurrentState = MovementStates.Dashing;
        else if (!_controller.isGrounded)
            CurrentState = MovementStates.Airborne;
        else if (_cameraController.IsAiming)
            CurrentState = MovementStates.Aiming;
        else if (_input.IsSprinting && _moveInput.y > 0f)
            CurrentState = MovementStates.Sprinting;
        else if (_moveInput.sqrMagnitude > 0.01f)
            CurrentState = MovementStates.Walking;
        else
            CurrentState = MovementStates.Idle;
    }

    private float SpeedControl()
    {
        float currentAcceleration = 0f;
        
        switch (CurrentState)
        {
            case MovementStates.Sprinting:
                currentAcceleration = _settings.SprintAcceleration;
                _currentMaxSpeed = _settings.SprintSpeed;
                break;
            
            case MovementStates.Walking:
                currentAcceleration = _settings.WalkAcceleration;
                _currentMaxSpeed = _settings.WalkSpeed;
                break;
            
            case MovementStates.Aiming:
                currentAcceleration = _settings.AimingAcceleration;
                _currentMaxSpeed = _settings.AimSpeed;
                break;
            
            case MovementStates.Idle:
                currentAcceleration = _settings.IdleAcceleration;
                _currentMaxSpeed = 0f;
                break;
            
            case MovementStates.Airborne:
                currentAcceleration = _settings.AirborneAcceleration;
                break;
        }
        
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, 
            _currentMaxSpeed, 
            currentAcceleration * Time.deltaTime);
        
        return _currentSpeed;
    }
    
    
    private void HandleJump()
    {
        JumpTimers();
        if (_jumpBufferTimer <= 0f || _coyoteTimeTimer <= 0f)
            return;
        
        _jumpBufferTimer = 0f;
        _coyoteTimeTimer = 0f;
        
        OnJump?.Invoke();
        _velocity.y = Mathf.Sqrt(_settings.MinJumpHeight * -2f * _settings.Gravity);
    }

    private void JumpTimers()
    {
        // Coyote timer
        if(_controller.isGrounded) 
            _coyoteTimeTimer = _settings.CoyoteTime;
        else
            _coyoteTimeTimer = Mathf.Max(0f, _coyoteTimeTimer - Time.deltaTime);
        
        //Jump Buffer timer
        if (_jumpBufferTimer > 0f)
            _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - Time.deltaTime);
    }

    private void AirState()
    {
        _isRising = !_controller.isGrounded && _velocity.y > 0.1f;
        _isFalling = !_controller.isGrounded && _velocity.y < -0.1f;
    }

    private void JumpPressed()
    {
        if(_dash.IsDashing)
            return;
        
        _jumpHold = true;
        _jumpBufferTimer = _settings.JumpBufferTime;
    }

    private void JumpReleased()
    {
        _jumpHold = false;
    }
    private void IntendToDash() => _dash.IntendToDash();
}