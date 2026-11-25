using UnityEngine;

public class TornPaperOfMagicSpellbook : ItemAbility
{
    [SerializeField] private int _damageAmount = 10;
    [SerializeField] private float _time = 3f;
    
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
        if (!target.GetComponent<DamageReceiver>()) return;

        damageInfo.DamageAmount = _damageAmount;
        damageInfo.DamageType = DamageType.Fire;
        
        target.GetComponent<DamageReceiver>().TakeDamageOverTime(damageInfo, _time);
        ReportActivated();
    }
}
