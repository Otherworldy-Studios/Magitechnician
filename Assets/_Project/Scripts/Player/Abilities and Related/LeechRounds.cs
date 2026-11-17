using UnityEngine;

public class LeechRounds : ItemAbility
{
    [SerializeField] private PlayerRoot _playerRoot;
    [SerializeField] private float _shieldPerHit;

    private void Awake()
    {
        if(!_playerRoot)
            _playerRoot = FindFirstObjectByType<PlayerRoot>();
    }

    private void OnEnable()
    {
        _playerRoot.Gun.OnShotHit += _playerRoot.Shield.Restore(_shieldPerHit);
    }

    private void OnDisable()
    {
        _playerRoot.Gun.OnShotHit -= _playerRoot.Shield.Restore(_shieldPerHit);
    }
}
