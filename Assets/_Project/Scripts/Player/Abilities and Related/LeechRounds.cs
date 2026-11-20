using UnityEngine;

public class LeechRounds : ItemAbility
{
    [SerializeField] private float _shieldPerHit;

    private void OnEnable()
    {
        PlayerGun.OnDamageDealt += HandleOnDamageDealt;
    }

    private void OnDisable()
    {
        PlayerGun.OnDamageDealt -= HandleOnDamageDealt;
    }

    private void HandleOnDamageDealt(DamageInfo damageInfo)
    {
        Player.Shield.Restore(_shieldPerHit);
    }
}
