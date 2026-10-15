using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    private InputSystem_Actions _input;
    
    public Vector3 Move => _input.Player.Move.ReadValue<Vector2>();
    public Vector3 Look => _input.Player.Look.ReadValue<Vector2>();
    public bool IsSprinting => _input.Player.Sprint.IsPressed();
    public bool IsAiming => _input.Player.Aim.IsPressed();
    public bool IsFiring => _input.Player.Fire.IsPressed();
    
    public event Action JumpPressed;
    public event Action DashPressed;
    public event Action FirePressed;

    private void Awake()
    {
        _input = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        _input.Player.Jump.performed += OnJump;
        _input.Player.Dash.performed += OnRoll;
        _input.Player.Fire.performed += OnFire;
        
        _input.Player.Enable();
    }

    private void OnDisable()
    {
        _input.Player.Jump.performed -= OnJump;
        _input.Player.Dash.performed -= OnRoll;
        _input.Player.Fire.performed -= OnFire;
        
        _input.Player.Disable();
    }
    
    private void OnDestroy()
    {
        _input.Dispose();
    }
    
    //That is bad unless player can't change WASD input
    private bool IsMovementInputPressed()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return false;

        return keyboard.wKey.isPressed ||
               keyboard.aKey.isPressed ||
               keyboard.sKey.isPressed ||
               keyboard.dKey.isPressed;
    }
    
    private void OnJump(InputAction.CallbackContext context) => JumpPressed?.Invoke();

    private void OnRoll(InputAction.CallbackContext context) => DashPressed?.Invoke();

    private void OnFire(InputAction.CallbackContext context) => FirePressed?.Invoke();
}
