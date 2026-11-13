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
    /// <summary>The movement state picked this frame.</summary>
    public MovementStates CurrentState { get; private set; }

    [SerializeField] private PlayerMovementSettings _settings;

    [Header("References")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private PlayerInput _input;
    [SerializeField] private PlayerCameraController _cameraController;
    [SerializeField] private Dash _dash;

    /// <summary>Vertical velocity from gravity and jumping. Horizontal movement isn't stored here.</summary>
    public Vector3 Velocity => _velocity;
    public bool IsRising => _isRising;
    public bool IsFalling => _isFalling;
    public bool IsGrounded => _controller.isGrounded;
    /// <summary>Top walking speed. Same as MoveSpeed.</summary>
    public float MaxSpeed => MoveSpeed;

    /// <summary>
    /// The move speed stat from the player's Stats, or _settings.LocomotionSpeed if the stat isn't set.
    /// </summary>
    // INTEGRATION: new. Charms and consumables change speed through StatModifiers on Stats, which PlayerRoot hands over
    // through SetStats.
    public float MoveSpeed => ReadStat(_moveSpeedStat, _settings.LocomotionSpeed);

    /// <summary>Fires when a jump starts. PlayerAnimator plays the jump animation.</summary>
    public event Action OnJumped;

    private CharacterController _controller;
    private Vector2 _moveInput;          // This frame's Move input.
    private Vector3 _velocity;           // Only y is used: gravity and jumps.
    private float _currentSpeed;         // Horizontal speed, which eases toward _currentMaxSpeed.
    private float _currentMaxSpeed = 0f; // The speed the current state allows. Kept while airborne.

    private bool _isRising;
    private bool _isFalling;

    private float _jumpBufferTimer; // Time left in which a Jump press still counts. Lets a press just before landing jump.
    private float _coyoteTimeTimer; // Time left in which a jump still works after walking off a ledge.
    private bool _jumpHold;         // True while Jump is held. Lowers gravity while rising, for a higher jump.

    // INTEGRATION: new. Source of the move speed stat. Set here or by PlayerRoot through SetStats.
    [SerializeField] private Stats _playerStats;
    [SerializeField] private StatDefinition _moveSpeedStat; // Which stat in _playerStats is the move speed.

    /// <summary>Hands over the player's Stats, unless one is already assigned.</summary>
    // INTEGRATION: new. PlayerRoot calls this in Awake, so the stat source is set in one place.
    public void SetStats(Stats playerStats)
    {
        if (_playerStats == null)
        {
            _playerStats = playerStats;
        }
    }

    /// <summary>Finds any references left empty.</summary>
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

    /// <summary>Runs one frame of movement, in order: air state, gravity, jump, dash, state, then the move.</summary>
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

    /// <summary>Listens to PlayerInput's jump and dash events.</summary>
    private void OnEnable()
    {
        _input.OnJumpPressed += HandleJumpPressed;
        _input.OnJumpReleased += HandleJumpReleased;
        _input.OnDashPressed += IntendToDash;
    }

    /// <summary>Stops listening to PlayerInput.</summary>
    private void OnDisable()
    {
        _input.OnJumpPressed -= HandleJumpPressed;
        _input.OnJumpReleased -= HandleJumpReleased;
        _input.OnDashPressed -= IntendToDash;
    }

    /// <summary>
    /// Converts movement input into a horizontal, camera-relative direction.
    /// </summary>
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
    /// Applies gravity and reduces it while the jump button is held during ascent.
    /// </summary>
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

    /// <summary>
    /// Selects the movement state by priority: dash, airborne, aim, sprint, walk, idle.
    /// </summary>
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
    /// Smoothly approaches the speed allowed by the current movement state.
    /// Airborne movement retains the previous target speed.
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
                // INTEGRATION: was AimingAcceleration and AimSpeed. Aiming now moves at full speed, so the speed stat
                // applies while aiming too.
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

        _currentSpeed = Mathf.MoveTowards(_currentSpeed,
            _currentMaxSpeed,
            currentAcceleration * Time.deltaTime);

        return _currentSpeed;
    }

    /// <summary>
    /// Starts a jump when buffered input and coyote time overlap.
    /// Initial upward velocity is calculated from the minimum jump height.
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

    /// <summary>
    /// Refreshes coyote time while grounded and counts down the jump input buffer.
    /// </summary>
    private void JumpTimers()
    {
        // Coyote timer
        if (_controller.isGrounded)
            _coyoteTimeTimer = _settings.CoyoteTime;
        else
            _coyoteTimeTimer = Mathf.Max(0f, _coyoteTimeTimer - Time.deltaTime);

        // Jump buffer timer
        if (_jumpBufferTimer > 0f)
            _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - Time.deltaTime);
    }

    /// <summary>Sets _isRising and _isFalling from the vertical velocity.</summary>
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

    /// <summary>Marks Jump as released, which ends the lower gravity of a held jump.</summary>
    private void HandleJumpReleased()
    {
        _jumpHold = false;
    }

    /// <summary>Passes a Dash press to Dash, which decides whether it can dash.</summary>
    private void IntendToDash() => _dash.IntendToDash();

    /// <summary>Returns a stat from _playerStats, or the fallback when the stat or Stats isn't set.</summary>
    // INTEGRATION: new. The same lookup as EnemyBase.ReadStat.
    private float ReadStat(StatDefinition stat, float fallback)
    {
        if (_playerStats == null || stat == null || !_playerStats.HasStat(stat))
            return fallback;

        return _playerStats.GetValue(stat);
    }
}