using UnityEngine;

namespace _Project.Scripts.Dungeon
{
    /// <summary>
    /// A room's exit. While active, shows the room's rewards if any are waiting, then moves the player to the connected
    /// room's spawn point and enters that room. Room.OnComplete turns it on. Needs a trigger BoxCollider on the same
    /// object, on a layer that collides with Player.
    /// </summary>
    public class Portal : MonoBehaviour
    {
        [Header("Debug (Do not edit)")]
        [Tooltip("The room this portal is in. Set by Room.Awake.")]
        [SerializeField] private Room _parent;

        [Tooltip("The room it leads to. Set by the generator. Null on the End room.")]
        [SerializeField] private Room _connected;

        [Tooltip("False until the room is cleared. False by default in the prefab.")]
        [SerializeField] private bool _active;

        [Tooltip("The portal effect, shown while the portal is active. Optional.")]
        [SerializeField] private GameObject _vfx;

        [Tooltip("The trigger the player walks into. Found on this object in Start.")]
        [SerializeField] private BoxCollider _collider;

        private bool _waitingOnRewards; // True while the reward screen is open, so walking in again doesn't start a second teleport.

        public Room Parent { get { return _parent; } set { _parent = value; } }
        public Room Connected { get { return _connected; } set { _connected = value; } }
        public bool Active { get { return _active; } set { _active = value; } }

        private void Start()
        {
            // Doesn't set _active to false here. Start can run after the room is already cleared, which would turn the
            // portal back off.
            _collider = GetComponent<BoxCollider>();
        }


        private void Update()
        {
            if (_vfx != null && _vfx.activeSelf != Active)
                _vfx.SetActive(Active);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Portals that lead nowhere, such as the End room's, do nothing.
            if (!_active || _connected == null || _waitingOnRewards)
                return;

            // Only the player has a PlayerSpawner, so enemies and projectiles don't teleport.
            PlayerSpawner spawner = other.GetComponentInParent<PlayerSpawner>();
            if (spawner == null)
                return;

            if (GameManager.HasInstance && GameManager.Instance.TryShowRewards(_parent, () => Teleport(spawner)))
            {
                _waitingOnRewards = true;
                return;
            }

            Teleport(spawner);
        }

        /// <summary>
        /// Moves the player to the connected room and enters it. PlayerSpawner turns the CharacterController off for the
        /// move and turns the camera to face the spawn point. EnterRoom updates GameManager.CurrentRoom and starts the
        /// room's waves.
        /// </summary>
        private void Teleport(PlayerSpawner spawner)
        {
            _waitingOnRewards = false;

            if (spawner == null)
                return;

            spawner.TeleportToRoom(_connected);
            _connected.EnterRoom();
        }

        private void OnDrawGizmos()
        {
            // A line toward the connected room, and a box at the portal.
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