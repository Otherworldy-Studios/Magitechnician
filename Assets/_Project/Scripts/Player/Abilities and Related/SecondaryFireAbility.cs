using UnityEngine;

/// <summary>
/// Base class for secondary fire abilities.
/// Handles player input and cooldown before executing the ability.
/// </summary>
public abstract class SecondaryFireAbility : ItemAbility
{
    [Header("Settings")]
    [SerializeField, Min(0f)] private float _cooldown = 1f;

    // Found through PlayerRoot, because ability prefabs are created under the player at runtime.
    private PlayerInput _playerInput;
    private Shoot _shoot;
    private float _nextFireTime;

    protected override void Awake()
    {
        base.Awake();
        if (Player == null) return;

        _playerInput = Player.GetComponentInChildren<PlayerInput>();
        _shoot = Player.GetComponentInChildren<Shoot>();

        if (_playerInput == null) Debug.LogError("No PlayerInput found under the player", this);
        if (_shoot == null) Debug.LogError("No Shoot found under the player", this);
    }

    protected virtual void OnEnable()
    {
        if (_playerInput != null)
            _playerInput.OnSecondaryFirePressed += HandleSecondaryFire;
    }

    protected virtual void OnDisable()
    {
        if (_playerInput != null)
            _playerInput.OnSecondaryFirePressed -= HandleSecondaryFire;
    }

    private void HandleSecondaryFire()
    {
        if (InputManager.Instance.Mode != InputMode.Gameplay) return;
        if (Time.time < _nextFireTime) return;
        if (_shoot == null || PlayerGun == null || PlayerGun.Muzzle == null) return;

        Vector3 origin = PlayerGun.Muzzle.position;
        Vector3 direction = _shoot.LastAim.Direction;

        Fire(origin, direction);
        ReportActivated();

        _nextFireTime = Time.time + _cooldown;
    }

    protected abstract void Fire(Vector3 origin, Vector3 direction);
}