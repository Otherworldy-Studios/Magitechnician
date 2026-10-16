using UnityEngine;

//Deals damage equal to the entity's baseDamage stat from Stats. Can be modified by charms and consumables. Can be used by any entity type (Player, Enemy, etc.).
public class Weapon : MonoBehaviour
{
    [SerializeField] private Stats _stats;

    //the owner's health is needed to check if the owner is dead before allowing the weapon to hit.
    private Health _ownerHealth;

    public float damage => _stats != null ? _stats.baseDamage : 0f;

    private void Awake()
    {
        //Searches this GameObject first, then its parents, so the weapon can be a child of the entity.
        if (_stats == null)
        {
            _stats = GetComponentInParent<Stats>();
        }
        if (_stats == null)
        {
            Debug.LogError($"{name}: Weapon requires a Stats component on itself or a parent.", this);
            return;
        }

        _ownerHealth = _stats.GetComponent<Health>();
    }

    public void Hit(Health target)
    {
        if (_stats == null)
            return;

        if (target == null)
        {
            Debug.LogWarning($"{name}: Weapon cannot hit a null target.", this);
            return;
        }

        //Touching the owner is normal with colliders, so ignore it silently.
        if (target.gameObject == _stats.gameObject)
            return;

        //Dead entities can't attack.
        if (_ownerHealth != null && _ownerHealth.isDead)
            return;

        target.TakeDamage(damage);
    }
}
