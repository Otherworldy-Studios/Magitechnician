using UnityEngine;

/// <summary>
/// Determines whether an enemy has an unobstructed view of its target
/// </summary>

[RequireComponent(typeof(EnemyBase))]
public class EnemyLineOfSight : MonoBehaviour, IEnemyLineOfSight
{
    [SerializeField] private float eyeHeight = 1f;
    private EnemyBase enemy;
    private void Awake()
    {
        enemy = GetComponent<EnemyBase>();
    }
    public bool HasLineOfSight()
    {
        if (enemy.Target == null)
            return false;

        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        
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
            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestObject = hit.transform;
            }
        }
        
        return closestObject != null && closestObject.IsChildOf(enemy.Target);
    }
}
