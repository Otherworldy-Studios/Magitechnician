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

        [SerializeField] public GameObject vfx;
        
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
            if(Active)
            {
                vfx.SetActive(true);
            }
            else
            {
                vfx.SetActive(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_active)
                return;
            
            //Debug.Log($"{other.gameObject.name} collided with the portal");

            if (other.gameObject.GetComponentInChildren<CharacterController>() != null)
            {
                CharacterController controller = other.gameObject.GetComponentInChildren<CharacterController>();
                Debug.Log("Player has entered the portal");

                controller.enabled = false;
                controller.gameObject.transform.position = _connected.PlayerSpawnPos;
                
                Physics.SyncTransforms();
                controller.enabled = true;
            }
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