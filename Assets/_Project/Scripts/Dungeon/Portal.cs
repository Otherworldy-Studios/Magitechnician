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
        
        public Room Parent {  get { return _parent; } set { _parent = value; } }
        
        [SerializeField]
        private Room _connected;
        
        public Room Connected { get { return _connected; } set { _connected = value; } }

        [SerializeField]
        private bool _active;
        public bool Active { get { return _active; } set { _active = value; } }
        
        //[SerializeField] If there's only one portal in each room, this isn't really needed
        //private LevelGenerator.Direction _direction;
        
        [SerializeField]
        private BoxCollider _collider;
        
        void Start()
        {
            _active = false;
            _collider = GetComponent<BoxCollider>();
        }
        
        void Update()
        {
            
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!_active)
                return;
            
            if (hit.gameObject.CompareTag("Player")) // Player needs the Player tag
                hit.gameObject.transform.position = Connected.PlayerSpawnPos;
        }
        
        private void OnDrawGizmos()
        {
            // Draws the portal gizmo
            if (Connected != null) 
            {
                Gizmos.color = Color.cyan;
                Vector3 destination = gameObject.transform.position;

                destination += (Connected.transform.position - gameObject.transform.position).normalized;
                Gizmos.DrawLine(gameObject.transform.position, destination);
            }

            Vector3 boxSize = new Vector3(1f, 1f, 1f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, boxSize);
        }
    }
}