using UnityEngine;

namespace _Project.Scripts.Dungeon
{
    public class Spawnpoint : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            // Draws the spawnpoint gizmo
            Gizmos.color = new Color(0f, 0.9f, 1f, 0.5f);
            
            Gizmos.DrawSphere(transform.position, 1.5f);
        }
    }
}