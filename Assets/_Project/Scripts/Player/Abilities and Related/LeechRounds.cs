using UnityEngine;

/// <summary>Restores shield each time one of the player's bullets damages something.</summary>
public class LeechRounds : ItemAbility
{
    [SerializeField] private float _shieldPerHit;

    // ItemAbility.Awake already logged an error if there's no PlayerRoot, so these only skip.
    private void OnEnable()
    {
        if (PlayerGun == null) return;

        PlayerGun.OnDamageDealt += HandleDamageDealt;
    }

    private void OnDisable()
    {
        if (PlayerGun == null) return;

        PlayerGun.OnDamageDealt -= HandleDamageDealt;
    }

    private void HandleDamageDealt(GameObject target, DamageInfo damageInfo)
    {
        if (Player.Shield == null) return;

        Player.Shield.Restore(_shieldPerHit);
    }
}