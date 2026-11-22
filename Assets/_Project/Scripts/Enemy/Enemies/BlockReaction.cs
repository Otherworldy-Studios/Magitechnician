using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// The Dark Knight's block. Each time the player fires, rolls Block Chance. On a success, while the Dark Knight has a
/// target, faces the player, isn't attacking, and is off cooldown, it turns on the shield collider for Block Duration.
/// The shield is a BlockShield in front of the Dark Knight, so shots from the side or behind still hit.
/// Plays the Block animation, and slows the Dark Knight while blocking.
/// </summary>
[RequireComponent(typeof(EnemyBase))]
public class BlockReaction : MonoBehaviour
{
    [Tooltip("The collider on the BlockShield object. Turned on only while blocking.")]
    [SerializeField] private Collider _shieldCollider;

    [Header("Timing")]
    [Tooltip("Chance to block each time the player fires, from 0 to 1.")]
    [SerializeField, Range(0f, 1f)] private float _blockChance = 0.3f;
    [Tooltip("Seconds the shield stays up.")]
    [SerializeField, Min(0f)] private float _blockDuration = 1f;
    [Tooltip("Seconds after a block ends before the next one can start.")]
    [SerializeField, Min(0f)] private float _cooldown = 3f;

    [Header("Conditions")]
    [Tooltip("Largest angle between the Dark Knight's forward and the player for it to block.")]
    [SerializeField, Range(0f, 180f)] private float _maxAngleToPlayer = 60f;
    [Tooltip("Times the move speed while blocking. 0 stops the Dark Knight.")]
    [SerializeField, Range(0f, 1f)] private float _speedMultiplierWhileBlocking = 0.2f;
    [SerializeField] private GameObject _physicalShield;
    [SerializeField] private Animator _physicalShieldAC;

    private EnemyBase _enemy;
    private Gun _playerGun;      // The registered player's gun, whose OnFired starts a block roll.
    private float _blockEndTime; // Time.time when the current block ends.
    private float _nextBlockTime; // Time.time when the cooldown ends.



    public bool IsBlocking { get; private set; }

    /// <summary>Fires when the shield goes up. For the block sound.</summary>
    public event Action OnBlockStarted;
    public event Action OnBlockEnded;
    /// <summary>Fires when the shield stops a projectile, with the hit point. For the spark and the clang.</summary>
    public event Action<Vector3> OnBulletBlocked;

    private void Awake()
    {
        _enemy = GetComponent<EnemyBase>();
        _physicalShieldAC = _physicalShield.GetComponent<Animator>();
        SetShield(false);
    }

    private void OnEnable()
    {
        if (!GameManager.HasInstance)
            return;

        GameManager.Instance.OnPlayerRegistered += HandlePlayerRegistered;

        GameObject player = GameManager.Instance.Player;
        if (player != null)
            HandlePlayerRegistered(player.GetComponent<PlayerRoot>());
    }

    private void OnDisable()
    {
        if (GameManager.HasInstance)
            GameManager.Instance.OnPlayerRegistered -= HandlePlayerRegistered;

        UnbindGun();
        EndBlock();
    }

    private void Update()
    {
        if (IsBlocking && (Time.time >= _blockEndTime || _enemy.IsDead))
            EndBlock();
    }

    /// <summary>Fires OnBulletBlocked. BlockShield calls this when it stops a projectile.</summary>
    public void ReportBulletBlocked(Vector3 point)
    {
        OnBulletBlocked?.Invoke(point);
    }

    /// <summary>Listens to the new player's gun, so each shot can start a block.</summary>
    private void HandlePlayerRegistered(PlayerRoot player)
    {
        UnbindGun();

        _playerGun = player != null ? player.Gun : null;

        if (_playerGun != null)
            _playerGun.OnFired += HandlePlayerFired;
    }

    private void UnbindGun()
    {
        if (_playerGun != null)
            _playerGun.OnFired -= HandlePlayerFired;

        _playerGun = null;
    }

    private void HandlePlayerFired()
    {
        if (!CanBlock() || Random.value >= _blockChance)
            return;

        StartBlock();
    }

    /// <summary>
    /// True when the Dark Knight is alive, has a target in front of it, isn't attacking or stunned, and is off cooldown.
    /// </summary>
    private bool CanBlock()
    {
        if (IsBlocking || _enemy.IsDead || !_enemy.HasTarget || _enemy.IsAttacking || Time.time < _nextBlockTime)
            return false;

        if (_enemy.CurrentState == _enemy.StunnedState)
            return false;

        Vector3 toPlayer = _enemy.Target.position - transform.position;
        toPlayer.y = 0f;

        return toPlayer.sqrMagnitude < 0.0001f || Vector3.Angle(transform.forward, toPlayer) <= _maxAngleToPlayer;
    }

    private void StartBlock()
    {
        IsBlocking = true;
        _blockEndTime = Time.time + _blockDuration;

        SetShield(true);
        _enemy.Movement.SpeedMultiplier = _speedMultiplierWhileBlocking;
        _enemy.PlayAnimation(EnemyAnimation.Block);
        _physicalShieldAC.Play("Block");
        OnBlockStarted?.Invoke();
    }

    /// <summary>Lowers the shield, starts the cooldown, and goes back to the current state's animation.</summary>
    private void EndBlock()
    {
        if (!IsBlocking)
            return;

        IsBlocking = false;
        _nextBlockTime = Time.time + _cooldown;

        SetShield(false);

        if (_enemy.Movement != null)
            _enemy.Movement.SpeedMultiplier = 1f;

        if (!_enemy.IsDead)
            _enemy.ReturnToStateAnimation();

        OnBlockEnded?.Invoke();
    }

    private void SetShield(bool on)
    {
        if (_shieldCollider != null)
            _shieldCollider.enabled = on;
        _physicalShield.SetActive(on);
    }
}