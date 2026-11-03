using UnityEngine;

public enum PlayerCombatStates
{
    Relaxed,
    Aiming,
    HipFire
}

public class PlayerCombatState : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private float _hipFireDuration = 1.5f;
    
    [Header("References")]
    [SerializeField] private Shoot _shoot;
    [SerializeField] private PlayerCameraController _playerCameraController;
    [SerializeField] private Dash _dash;

    [field: SerializeField] public PlayerCombatStates CurrentCombatState { get; private set; }
    
    private float _lastShotTime = float.NegativeInfinity;

    private void OnEnable()
    {
        _shoot.OnFire += HandleFire;
    }

    private void OnDisable()
    {
        _shoot.OnFire -= HandleFire;
    }

    private void Update()
    {
        if (_dash.IsDashing && !_playerCameraController.IsAiming)
        {
            _lastShotTime = float.NegativeInfinity;
            CurrentCombatState = PlayerCombatStates.Relaxed;
            return;
        }

        if (_playerCameraController.IsAiming)
        {
            CurrentCombatState = PlayerCombatStates.Aiming;
        } else if (Time.time - _lastShotTime < _hipFireDuration)
        {
            CurrentCombatState = PlayerCombatStates.HipFire;
        }
        else
        {
            CurrentCombatState = PlayerCombatStates.Relaxed;
        }
    }

    private void HandleFire()
    {
        _lastShotTime = Time.time;
        CurrentCombatState = _playerCameraController.IsAiming
            ? PlayerCombatStates.Aiming
            : PlayerCombatStates.HipFire;
    }
}
