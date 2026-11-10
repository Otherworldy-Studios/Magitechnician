using UnityEngine;

namespace _Project.Scripts.Dungeon
{
    /// <summary>Marks a spot in a room where an enemy can spawn. Only draws a gizmo for now.</summary>
    public class Spawnpoint : MonoBehaviour
    {
        /// <summary>Draws a translucent sphere at the spawn point in the Scene view.</summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 0.9f, 1f, 0.5f);

            Gizmos.DrawSphere(transform.position, 1.5f);
        }
    }
}
