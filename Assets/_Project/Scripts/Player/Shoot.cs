using System;
using UnityEngine;

public class Shoot : MonoBehaviour
{
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private PlayerInput _input;
    [SerializeField] private PlayerAimController _aimController;
    [SerializeField] private PlayerCameraController _cameraController;
    
    public event Action OnFire;

    private void Awake()
    {
        if(!_input) 
            _input = FindFirstObjectByType<PlayerInput>();
        if(!_aimController)
            _aimController = FindFirstObjectByType<PlayerAimController>();
        if(!_cameraController)
            _cameraController = FindFirstObjectByType<PlayerCameraController>();
        if(!_spawnPoint)
            Debug.LogWarning("No Spawn Point set on " + gameObject.name);
        if(!_bulletPrefab)
            Debug.LogWarning("No Bullet Prefab set on " + gameObject.name);
    }

    private void OnEnable()
    {
        _input.FirePressed += Fire;
    }

    private void OnDisable()
    {
        _input.FirePressed -= Fire;
    }

    private void Fire()
    {
        if (!_cameraController.IsAiming)
            return;
        
        Vector3 direction = _aimController.ShotDirection;

        Quaternion rotation = Quaternion.LookRotation(direction);

        GameObject bullet = Instantiate(_bulletPrefab, _spawnPoint.position, rotation);
        Projectile projectile = bullet.GetComponent<Projectile>();
        projectile.Initialize(_input.gameObject); // 100% on main player object
        
        OnFire?.Invoke();
    }
}
