using System;
using UnityEngine;

// CHANGED: rewritten to use CrossHairAim and the Gun.
// Each frame it works out the aim with CrossHairAim.Solve, stores it in LastAim for the crosshair dot,
// and passes it to Gun.TryFire. The gun's fire mode decides whether a shot happens that frame.
public class Shoot : MonoBehaviour
{
    [SerializeField] private Gun _gun;                                 
    [SerializeField] private InputManager _input;
    [SerializeField] private Camera _camera;                            
    [SerializeField] private LayerMask _hitMask;                      
    [SerializeField, Range(1f, 90f)] private float _maxMuzzleAngle = 90f; 
    [SerializeField] private PlayerCameraController _cameraController;
    

    public event Action OnFire;

   
    public AimSolution LastAim { get; private set; }
    public bool HasAim { get; private set; } 

    private GameObject _owner;

    private void Awake()
    {
        if (!_input)
            _input = InputManager.Instance;
        if (!_cameraController)
            _cameraController = FindFirstObjectByType<PlayerCameraController>();
        if (!_gun)                                                  
            _gun = GetComponentInChildren<Gun>();                   
        if (!_gun)                                                   
            Debug.LogWarning("No Gun found for " + gameObject.name); 
        if (!_camera)                                                
            _camera = Camera.main;                                  

        PlayerRoot root = GetComponentInParent<PlayerRoot>();       
        _owner = root != null ? root.gameObject : gameObject;      
    }

    private void OnEnable()
    {
        _input.FirePressed += PullTrigger;   
    }

    private void OnDisable()
    {
        _input.FirePressed -= PullTrigger;     
        if (_gun) _gun.ReleaseTrigger();       
    }

    
    private void Update()
    {
        if (!_gun || !_gun.Muzzle || !_camera) return;

        // The aim starts from the camera's ray through the screen centre, so the shot lands under the crosshair.
        // The muzzle is only used as the bullet's starting point, and only when it can reach that point.
        LastAim = CrossHairAim.Solve(
            _camera,
            _gun.Muzzle.position,
            _gun.Range,
            _hitMask,
            _owner,
            _gun.BulletRadius,
            _maxMuzzleAngle);
        HasAim = true;

        
        if (!_input.IsFiring) _gun.ReleaseTrigger();

        
        if (_gun.TryFire(LastAim))
        {
            OnFire?.Invoke();
        }
    }

  
    private void PullTrigger() { if (_gun) _gun.PullTrigger(); }
}