using UnityEngine;

/// <summary>
/// Determines whether an enemy has an unobstructed view of its target
/// </summary>

[RequireComponent(typeof(EnemyBase))]
public class LineOfSight : MonoBehaviour, IEnemyLineOfSight
{
    [SerializeField] private Transform _eyes;
    [SerializeField] private LayerMask _blockingLayers = ~0;
    private EnemyBase enemy;
    private void Awake()
    {
        enemy = GetComponent<EnemyBase>();
    }
    public bool HasLineOfSight()
    {
        if (enemy.Target == null)
            return false;

        Vector3 origin = _eyes.position;
        
        CharacterController controller =
            enemy.Target.GetComponent<CharacterController>();

        if (controller == null)
            return false;

        Vector3 destination = controller.bounds.center;

        Vector3 direction = destination - origin;
        float distance = direction.magnitude;

        if (distance <= 0f)
            return true;

        // Check all hits along the ray
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction.normalized,
            distance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = Mathf.Infinity;
        Transform closestObject = null;

        foreach (RaycastHit hit in hits)
        {
            // Ignore the enemy's own colliders
            if(hit.transform.IsChildOf(transform))
                continue;
                
            // Determine whether the ray hit the player
            bool isPlayer = hit.transform.IsChildOf(enemy.Target);

            // Check whether the object's layer blocks LOS
            bool isBlockingLayer = (_blockingLayers.value & (1 << hit.collider.gameObject.layer)) != 0;

            //Ignore nonblocking objects but consider the player
            if(!isPlayer && !isBlockingLayer)
                continue;
            
            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestObject = hit.transform;
            }
        }
        
        return closestObject != null && closestObject.IsChildOf(enemy.Target);
    }
}
