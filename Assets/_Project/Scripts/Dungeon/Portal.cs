using UnityEngine;
using UnityEngine.Serialization;

namespace _Project.Scripts.Dungeon
{
    /// <summary>
    /// A room's exit. While active, moves the player to the connected room's spawn point and enters that room.
    /// Room.OnComplete turns it on. Needs a trigger BoxCollider on the same object, on a layer that collides with Player.
    /// </summary>
    public class Portal : MonoBehaviour
    {
        [Header("Debug (Do not edit)")]
        [SerializeField]
        private Room _parent; // The room this portal is in. Set by Room.Awake.

        public Room Parent { get { return _parent; } set { _parent = value; } }

        [SerializeField]
        private Room _connected; // The room it leads to. Set by the generator. Null on the End room.

        public Room Connected { get { return _connected; } set { _connected = value; } }

        [SerializeField]
        private bool _active; // False until the room is cleared. False by default in the prefab.

        public bool Active { get { return _active; } set { _active = value; } }

        [Tooltip("The portal effect, shown while the portal is active. Optional.")]
        [SerializeField, FormerlySerializedAs("vfx")] private GameObject _vfx;

        [SerializeField]
        private BoxCollider _collider; // The trigger the player walks into.

        /// <summary>Finds the trigger collider.</summary>
        private void Start()
        {
            // INTEGRATION: removed "_active = false". Start runs the frame after generation, so it turned the start
            // room's portal back off right after LevelGenerator.FinishGeneration opened it.
            _collider = GetComponent<BoxCollider>();
        }

        /// <summary>Shows the effect while the portal is active.</summary>
        private void Update()
        {
            // INTEGRATION: null check, and only switches on a change. Without a vfx assigned, this threw every frame.
            if (_vfx != null && _vfx.activeSelf != Active)
                _vfx.SetActive(Active);
        }

        /// <summary>
        /// Moves the player through the portal. Ignores everything while inactive, on a portal that leads nowhere,
        /// and anything that isn't the player.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!_active || _connected == null) // INTEGRATION: also skips portals that lead nowhere, such as the End room's.
                return;

            // INTEGRATION: only the player has a PlayerSpawner, so enemies and projectiles no longer teleport.
            PlayerSpawner spawner = other.GetComponentInParent<PlayerSpawner>();
            if (spawner == null)
                return;

            // INTEGRATION: replaces moving the CharacterController here. PlayerSpawner turns it off for the move, faces the
            // player toward the spawn point, and turns the camera to match. EnterRoom updates GameManager.CurrentRoom and
            // starts the room's RoomEncounter.
            spawner.TeleportToRoom(_connected);
            _connected.EnterRoom();
        }

        /// <summary>Draws the portal's position and a line toward the connected room in the Scene view.</summary>
        private void OnDrawGizmos()
        {
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
