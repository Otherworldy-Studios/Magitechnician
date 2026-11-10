using System;
using _Project.Scripts.Dungeon;
using UnityEngine;

/// <summary>
/// Moves the player into rooms. Put this on the player prefab root, next to PlayerRoot.
///
/// In the Dungeon, GameManager.RegisterPlayer calls SpawnWhenMapReady and then requests generation.
/// When the LevelGenerator finishes and bakes the NavMesh, this moves the player to the Start room's
/// spawn point and fires OnPlayerSpawned. GameManager starts the run from that event.
///
/// Portal calls TeleportToRoom to move the player between rooms.
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [Tooltip("The player's CharacterController. Found in children if empty.")]
    [SerializeField] private CharacterController _controller;

    [Tooltip("Turned to match the spawn point after a teleport. Found in children, then in the scene, if empty.")]
    [SerializeField] private CameraTargetController _cameraTarget;

    [Tooltip("The camera pitch after a teleport, in degrees. 0 is level.")]
    [SerializeField] private float _pitchAfterTeleport = 0f;

    /// <summary>Fires after the player is placed in the Start room of a new map.</summary>
    public event Action OnPlayerSpawned;

    private LevelGenerator _generator;

    private void Awake()
    {
        if (_controller == null)
            _controller = GetComponentInChildren<CharacterController>();

        if (_cameraTarget == null)
            _cameraTarget = GetComponentInChildren<CameraTargetController>();
    }

    private void Start()
    {
        // The camera target may sit outside the player prefab.
        if (_cameraTarget == null)
            _cameraTarget = FindAnyObjectByType<CameraTargetController>();
    }

    /// <summary>
    /// Waits for the generator's next OnMapGenerated, then moves the player to its Start room.
    /// Call this before requesting generation, since generation finishes in the same frame.
    /// </summary>
    public void SpawnWhenMapReady(LevelGenerator generator)
    {
        if (generator == null)
        {
            Debug.LogError("PlayerSpawner: no LevelGenerator in this scene.", this);
            return;
        }

        StopListening();
        _generator = generator;
        _generator.OnMapGenerated += HandleMapGenerated;
    }

    /// <summary>Moves the player to the room's spawn point and turns them to face the way it faces.</summary>
    public void TeleportToRoom(Room room)
    {
        if (room == null) return;
        Teleport(room.PlayerSpawnPos, room.PlayerSpawnRotation);
    }

    /// <summary>
    /// Moves the player to a position and turns the player and camera to the rotation's yaw.
    /// The CharacterController is turned off for the move, since it overwrites position changes made while it's on.
    /// </summary>
    public void Teleport(Vector3 position, Quaternion rotation)
    {
        if (_controller == null)
        {
            Debug.LogError("PlayerSpawner: no CharacterController found on the player.", this);
            return;
        }

        float yaw = rotation.eulerAngles.y;
        Transform body = _controller.transform;

        bool wasEnabled = _controller.enabled;
        _controller.enabled = false;

        body.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        Physics.SyncTransforms();

        _controller.enabled = wasEnabled;

        if (_cameraTarget != null)
            _cameraTarget.SetYawPitch(yaw, _pitchAfterTeleport);
    }

    private void HandleMapGenerated()
    {
        Room startRoom = _generator != null ? _generator.StartRoom : null;
        StopListening();

        if (startRoom == null)
        {
            Debug.LogError("PlayerSpawner: the map finished without a Start room.", this);
            return;
        }

        TeleportToRoom(startRoom);
        OnPlayerSpawned?.Invoke();
    }

    private void StopListening()
    {
        if (_generator != null)
            _generator.OnMapGenerated -= HandleMapGenerated;

        _generator = null;
    }

    private void OnDestroy()
    {
        StopListening();
    }
}