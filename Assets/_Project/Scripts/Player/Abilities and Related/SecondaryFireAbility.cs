using UnityEngine;

/// <summary>
/// Base class for secondary fire abilities.
/// Handles player input and cooldown before executing the ability.
/// </summary>
public abstract class SecondaryFireAbility : ItemAbility
{
    [Header("References")]
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private Shoot _shoot;

    [Header("Settings")]
    [SerializeField, Min(0f)] private float _cooldown = 1f;

    private float _nextFireTime;

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
        if (InputManager.Instance.Mode != InputMode.Gameplay)
            return;

        if (Time.time < _nextFireTime)
            return;

        if (_shoot == null || PlayerGun.Muzzle == null)
            return;

        Vector3 origin = PlayerGun.Muzzle.position;
        Vector3 direction = _shoot.LastAim.Direction;

        Fire(origin, direction);

        _nextFireTime = Time.time + _cooldown;
    }

    protected abstract void Fire(Vector3 origin, Vector3 direction);
}