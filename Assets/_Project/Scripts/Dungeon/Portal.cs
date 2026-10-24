using UnityEngine;

namespace _Project.Scripts.Dungeon
{
    /// <summary>
    /// Portals connect rooms to each other in the game, allowing
    /// the player to travel between them
    /// </summary>
    public class Portal : MonoBehaviour
    {
        [Header("Debug (Do not edit)")]
        [SerializeField]
        private Room _parent;
        
        [SerializeField]
        private Portal _connected;
        
        [SerializeField]
        private LevelGenerator.Direction _direction;
        
        void Start()
        {
            
        }
        
        void Update()
        {
            
        }

        private void OnDrawGizmos()
        {
            // Draws the portal gizmo
            Gizmos.color = Color.cyan;
            Vector3 destination = gameObject.transform.position;

            switch (_direction)
            {
                case LevelGenerator.Direction.North:
                    destination += new Vector3(0, 0, 2);
                    break;
                case LevelGenerator.Direction.South:
                    destination += new Vector3(0, 0, -2);
                    break;
                case LevelGenerator.Direction.East:
                    destination += new Vector3(2, 0, 0);
                    break;
                case LevelGenerator.Direction.West:
                    destination += new Vector3(-2, 0, 0);
                    break;
            }
            
            Gizmos.DrawLine(gameObject.transform.position, destination);
            
            Vector3 boxSize = new Vector3(1f, 1f, 1f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, boxSize);
        }
    }
}