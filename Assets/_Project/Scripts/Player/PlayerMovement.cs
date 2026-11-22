using System;
using ScriptableSettings;
using UnityEngine;

/// <summary>
/// Moves the player with the CharacterController: walking relative to the camera, gravity, jumping with coyote time
/// and a jump buffer, and dashing through Dash. Picks a movement state each frame, which PlayerAnimator reads.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    /// <summary>What the player is doing this frame. StateController picks one in priority order.</summary>
    public enum MovementStates
    {
        Idle,
        Locomotion,
        Airborne,
        Dashing,
        Aiming
    }

    [SerializeField] private PlayerMovementSettings _settings;

    [Header("References")]
    [SerializeField] private Transform _cameraTransform;
    [Tooltip("Found on this object if empty.")]
    [SerializeField] private PlayerInput _input;
    [Tooltip("Found in the scene if empty.")]
    [SerializeField] private PlayerCameraController _cameraController;
    [Tooltip("Found on this object if empty.")]
    [SerializeField] private Dash _dash;

    [Header("Stats")]
    [Tooltip("Source of the move speed stat. Set here, or by PlayerRoot through SetStats.")]
    [SerializeField] private Stats _playerStats;
    [Tooltip("Which stat in Player Stats is the move speed.")]
    [SerializeField] private StatDefinition _moveSpeedStat;

    private CharacterController _controller;
    private Vector2 _moveInput;          // This frame's Move input.
    private Vector3 _velocity;           // Only y is used: gravity and jumps.
    private float _currentSpeed;         // Horizontal speed, which eases toward _currentMaxSpeed.
    private float _currentMaxSpeed = 0f; // The speed the current state allows. Kept while airborne.

    private bool _isRising;
    private bool _isFalling;
    private float _airTime;              // Seconds since the player left the ground. Passed to OnLanded.

    private float _jumpBufferTimer; // Time left in which a Jump press still counts. Lets a press just before landing jump.
    private float _coyoteTimeTimer; // Time left in which a jump still works after walking off a ledge.
    private bool _jumpHold;         // True while Jump is held. Lowers gravity while rising, for a higher jump.

    /// <summary>Fires when a jump starts. PlayerAnimator plays the jump animation.</summary>
    public event Action OnJumped;

    /// <summary>
    /// Fires on the frame the player touches the ground, with how many seconds they were in the air.
    /// Fires after every short drop too, so listeners pick their own minimum air time.
    /// </summary>
    public event Action<float> OnLanded;

    public MovementStates CurrentState { get; private set; }

    /// <summary>Vertical velocity from gravity and jumping. Horizontal movement isn't stored here.</summary>
    public Vector3 Velocity => _velocity;
    public bool IsRising => _isRising;
    public bool IsFalling => _isFalling;
    public bool IsGrounded => _controller.isGrounded;
    /// <summary>Top walking speed. Same as MoveSpeed.</summary>
    public float MaxSpeed => MoveSpeed;

    /// <summary>The move speed stat from the player's Stats, or the settings' LocomotionSpeed if the stat isn't set.</summary>
    public float MoveSpeed => ReadStat(_moveSpeedStat, _settings.LocomotionSpeed);

    /// <summary>Hands over the player's Stats, unless one is already assigned. PlayerRoot calls this in Awake.</summary>
    public void SetStats(Stats playerStats)
    {
        if (_playerStats == null)
        {
            _playerStats = playerStats;
        }
    }

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        if (!_input)
            _input = GetComponent<PlayerInput>();
        if (!_cameraController)
            _cameraController = FindFirstObjectByType<PlayerCameraController>();
        if (!_dash)
            _dash = GetComponent<Dash>();
    }

    private void OnEnable()
    {
        _input.OnJumpPressed += HandleJumpPressed;
        _input.OnJumpReleased += HandleJumpReleased;
        _input.OnDashPressed += IntendToDash;
    }

    private void OnDisable()
    {
        _input.OnJumpPressed -= HandleJumpPressed;
        _input.OnJumpReleased -= HandleJumpReleased;
        _input.OnDashPressed -= IntendToDash;
    }

    // One frame of movement, in order: air state, gravity, jump, dash, state, the move, then the landing check.
    private void Update()
    {
        _moveInput = _input.Move;

        AirState();
        HandleGravity();
        HandleJump();
        _dash.HandleDash(GetMovementDirection(), IsGrounded);
        StateController();
        ApplyMovement();
        CheckLanding();
    }

    /// <summary>Converts movement input into a horizontal, camera-relative direction.</summary>
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

    /// <summary>Moves the CharacterController by the dash or walking velocity plus the vertical velocity.</summary>
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

    /// <summary>
    /// Counts the time in the air, and fires OnLanded on the first grounded frame after it.
    /// Runs after ApplyMovement, because the CharacterController updates isGrounded in Move.
    /// </summary>
    private void CheckLanding()
    {
        if (!_controller.isGrounded)
        {
            _airTime += Time.deltaTime;
            return;
        }

        if (_airTime > 0f)
        {
            OnLanded?.Invoke(_airTime);
            _airTime = 0f;
        }
    }

    /// <summary>Applies gravity, and reduces it while the jump button is held during the rise.</summary>
    private void HandleGravity()
    {
        if (_controller.isGrounded && _velocity.y < 0f)
            _velocity.y = _settings.GroundGravity;

        float gravity = _settings.Gravity;

        if (_isRising && _jumpHold)
        {
            gravity *= _settings.MinJumpHeight / _settings.MaxJumpHeight;
        }

        _velocity.y += gravity * Time.deltaTime;
    }

    /// <summary>Selects the movement state by priority: dash, airborne, aim, walk, idle.</summary>
    private void StateController()
    {
        if (_dash.IsDashing)
            CurrentState = MovementStates.Dashing;
        else if (!_controller.isGrounded)
            CurrentState = MovementStates.Airborne;
        else if (_cameraController.IsAiming)
            CurrentState = MovementStates.Aiming;
        else if (_moveInput.sqrMagnitude > 0.01f)
            CurrentState = MovementStates.Locomotion;
        else
            CurrentState = MovementStates.Idle;
    }

    /// <summary>
    /// Eases toward the speed the current movement state allows. Airborne movement keeps the walking speed.
    /// </summary>
    private float SpeedControl()
    {
        float currentAcceleration = 0f;

        switch (CurrentState)
        {
            case MovementStates.Locomotion:
                currentAcceleration = _settings.LocomotionAcceleration;
                _currentMaxSpeed = MoveSpeed;
                break;

            case MovementStates.Aiming:
                // Aiming moves at full speed, so the speed stat applies while aiming too.
                currentAcceleration = _settings.LocomotionAcceleration;
                _currentMaxSpeed = MoveSpeed;
                break;

            case MovementStates.Idle:
                currentAcceleration = _settings.IdleAcceleration;
                _currentMaxSpeed = 0f;
                break;

            case MovementStates.Airborne:
                currentAcceleration = _settings.AirborneAcceleration;
                _currentMaxSpeed = MoveSpeed;
                break;
        }

        _currentSpeed = Mathf.MoveTowards(_currentSpeed, _currentMaxSpeed, currentAcceleration * Time.deltaTime);

        return _currentSpeed;
    }

    /// <summary>
    /// Starts a jump when the jump buffer and coyote time overlap. The starting upward velocity comes from the
    /// minimum jump height. Holding Jump lowers gravity for a higher jump (see HandleGravity).
    /// </summary>
    private void HandleJump()
    {
        JumpTimers();
        if (_jumpBufferTimer <= 0f || _coyoteTimeTimer <= 0f)
            return;

        _jumpBufferTimer = 0f;
        _coyoteTimeTimer = 0f;

        OnJumped?.Invoke();
        _velocity.y = Mathf.Sqrt(_settings.MinJumpHeight * -2f * _settings.Gravity);
    }

    /// <summary>Refreshes coyote time while grounded, and counts down the jump buffer.</summary>
    private void JumpTimers()
    {
        if (_controller.isGrounded)
            _coyoteTimeTimer = _settings.CoyoteTime;
        else
            _coyoteTimeTimer = Mathf.Max(0f, _coyoteTimeTimer - Time.deltaTime);

        if (_jumpBufferTimer > 0f)
            _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - Time.deltaTime);
    }

    private void AirState()
    {
        _isRising = !_controller.isGrounded && _velocity.y > 0.1f;
        _isFalling = !_controller.isGrounded && _velocity.y < -0.1f;
    }

    /// <summary>Starts the jump buffer and marks Jump as held. Ignored while dashing.</summary>
    private void HandleJumpPressed()
    {
        if (_dash.IsDashing)
            return;

        _jumpHold = true;
        _jumpBufferTimer = _settings.JumpBufferTime;
    }

    private void HandleJumpReleased()
    {
        _jumpHold = false;
    }

    private void IntendToDash() => _dash.IntendToDash();

    /// <summary>Returns a stat from _playerStats, or the fallback when the stat or Stats isn't set.</summary>
    private float ReadStat(StatDefinition stat, float fallback)
    {
        if (_playerStats == null || stat == null || !_playerStats.HasStat(stat))
            return fallback;

        return _playerStats.GetValue(stat);
    }
}