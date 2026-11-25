using UnityEngine;

/// <summary>
/// An attack that hits everything in a circle around a point once, such as the Stone Guardian's ground slam. Shows an
/// optional marker on the ground for the whole wind-up, sized to the radius, so the player can see where it lands and
/// dash out. Damage goes through Explosion, so each target is hit once.
/// </summary>
public class AreaAttack : EnemyAttack
{
    [Tooltip("Where the circle is centered, such as a point in front of the boss. Leave empty to use the enemy's position.")]
    [SerializeField] private Transform _center;
    [Tooltip("Meters from the center that are hit.")]
    [SerializeField, Min(0f)] private float _radius = 4f;
    [Tooltip("Layers that can be hit. Set to Player, so the attack doesn't hurt other enemies.")]
    [SerializeField] private LayerMask _hitLayers;

    [Header("Telegraph")]
    [Tooltip("Shown on the ground at the center during the wind-up, and scaled so 1 unit wide covers the radius. Optional.")]
    [SerializeField] private GameObject _markerPrefab;
    [Tooltip("Spawned at the center on the hit, such as dust and cracks. Optional.")]
    [SerializeField] private GameObject _impactEffectPrefab;
    [Tooltip("What counts as ground for the marker.")]
    [SerializeField] private LayerMask _groundLayers = ~0;

    private GameObject _marker;

    private Vector3 CenterPosition => _center != null ? _center.position : transform.position;

    protected override void StartWindUp()
    {
        if (_markerPrefab == null)
            return;

        _marker = Instantiate(_markerPrefab, GroundUnderCenter(), Quaternion.identity);
        _marker.transform.localScale = new Vector3(_radius * 2f, _marker.transform.localScale.y, _radius * 2f);
    }

    // AttackState keeps turning the enemy toward the player during the wind-up, so the marker follows the center.
    private void LateUpdate()
    {
        if (_marker != null)
            _marker.transform.position = GroundUnderCenter();
    }

    protected override void PerformAttack()
    {
        base.PerformAttack();
        DamageInfo damageInfo = new DamageInfo
        {
            DamageAmount = Damage,
            DamageSource = Owner.gameObject,
            DamageDirection = Vector3.zero,
            DamageType = DamageType.Physical
        };

        Explosion.Explode(CenterPosition, _radius, damageInfo, _hitLayers, Owner.transform);

        if (_impactEffectPrefab != null)
            Instantiate(_impactEffectPrefab, CenterPosition, Quaternion.identity);

        RemoveMarker();
    }

    // Also runs when the attack is canceled, so a canceled slam doesn't leave its marker behind.
    protected override void FinishAttack()
    {
        RemoveMarker();
    }

    /// <summary>The ground below the center, or the center itself when there's no ground within 5 meters.</summary>
    private Vector3 GroundUnderCenter()
    {
        Vector3 position = CenterPosition;

        if (Physics.Raycast(position + Vector3.up, Vector3.down, out RaycastHit hit, 6f, _groundLayers, QueryTriggerInteraction.Ignore))
            return hit.point;

        return position;
    }

    private void RemoveMarker()
    {
        if (_marker != null)
            Destroy(_marker);

        _marker = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(CenterPosition, _radius);
    }
}
