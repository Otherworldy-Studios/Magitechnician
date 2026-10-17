using System;
using ScriptableSettings;
using UnityEngine;

public class Dash : MonoBehaviour
{
    [SerializeField] private DashSettings _settings;
    
    [Header("References")]
    [SerializeField] private Transform _orientation;
    
    public bool IsDashing { get; private set; }
    public Vector3 DashDirection {get; private set;}
    
    public event Action OnDashed;
    
    private float _dashTimer;
    private float _nextDashTime;
    private bool _intendToDash;

    private void Awake()
    {
        if(!_orientation)
            Debug.LogError($"{nameof(Dash)} requires a {nameof(_orientation)}");
    }

    public Vector3 GetDashVelocity()
    {
        float progress = Mathf.Clamp01(_dashTimer / _settings.DashDuration);

        // Fast at the beginning gradually slows down
        float speedMultiplier = 2f * (1f - progress);

        float dashSpeed = (_settings.DashDistance / _settings.DashDuration) * speedMultiplier;

        return DashDirection * dashSpeed;
    }
    
    public void HandleDash(Vector3 direction, bool canDash = true)
    {
        if (IsDashing)
        {
            _intendToDash = false;

            _dashTimer += Time.deltaTime;

            if (_dashTimer >= _settings.DashDuration)
                IsDashing = false;

            return;
        }

        if (!_intendToDash)
            return;

        _intendToDash = false;

        if (!canDash || Time.time < _nextDashTime)
            return;

        // If there is no movement input dash to camera direction
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = _orientation.transform.forward;
            direction.y = 0f;
        }

        DashDirection = direction.normalized;
        _dashTimer = 0f;
        IsDashing = true;
        
        //Cooldown
        _nextDashTime = Time.time + _settings.DashDuration + _settings.DashCooldown;

        OnDashed?.Invoke();
    }

    public void CancelDash()
    {
        if (!IsDashing)
            return;

        IsDashing = false;
    }
    
    public void IntendToDash() => _intendToDash = true;
}
