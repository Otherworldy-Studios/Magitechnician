using _Project.Scripts.Dungeon.Utility;
using _Project.Scripts.Dungeon.Utility.System.Collections.Generic;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Dungeon
{
    /// <summary>
    /// A room prefab and how likely it is to be picked. Higher weights are picked more often.
    /// </summary>
    [Serializable]
    struct WeightedRoom
    {
        [SerializeField] private Room _room;

        [SerializeField] private int _weight;

        public Room Room { get { return _room; } }
        public int Weight { get { return _weight; } }
    }

    /// <summary>
    /// Builds the dungeon in the Level scene when GameManager raises OnGenerateRequested: places a start room, grows
    /// combat rooms out from it on a grid using a seed, links neighbors and portals, puts the End room next to the
    /// farthest room, completes the start room when it has no enemies, bakes the NavMesh, and fires OnMapGenerated.
    /// PlayerSpawner and GameManager start the run from that event.
    /// </summary>
    // INTEGRATION: now inherits MGR<LevelGenerator> (shouldPersist off, Level scene only). Generation waits for
    // GameManager.OnGenerateRequested, which RegisterPlayer raises once the Level player exists.
    public class LevelGenerator : MGR<LevelGenerator>
    {
        /// <summary>
        /// The four possible generation directions
        /// </summary>
        public enum Direction
        {
            North,
            South,
            East,
            West
        }
        // INTEGRATION: new. Baked in BakeNavMesh after every room is placed. Set its Use Geometry to Physics Colliders,
        // because baking Render Meshes in a build needs Read/Write on every model.
        [Tooltip("The surface to bake once the rooms are placed.")]
        [SerializeField, FormerlySerializedAs("navMeshSurface")] private NavMeshSurface _navMeshSurface;
        private LinearCongruentialGenerator _random;          // Seeded random numbers for the generation chances and direction order.
        private Dictionary<Direction, Vector3> _directions;   // World direction for each grid direction.
        private Room[,] _roomGrid;                             // The map layout. Each cell holds a room or null.

        /// <summary>
        /// Fires after the map is built, the start room is handled, and the NavMesh is baked.
        /// PlayerSpawner places the player from it, and that starts the run.
        /// </summary>
        public event Action OnMapGenerated;

        /// <summary>The map layout. Each cell holds a room or null. GameManager copies it when the run starts.</summary>
        public Room[,] RoomGrid { get { return _roomGrid; } }
        /// <summary>The room the player starts in.</summary>
        public Room StartRoom => _startRoom;

        [Header("Generator Settings")]
        [Tooltip("Grid size. The start room is in the middle.")]
        [SerializeField] private int _roomGridWidth;
        [SerializeField] private int _roomGridHeight;

        [Tooltip("Combat rooms always generated before chance decides.")]
        [SerializeField] private int _minCombatRooms;

        [Tooltip("Generation stops at this many combat rooms.")]
        [SerializeField] private int _maxCombatRooms;

        [Tooltip("The same seed builds the same map.")]
        [SerializeField] private int _seed;

        [Tooltip("Picks a random seed each time instead of using the one above.")]
        [SerializeField] private bool _generateSeed;

        [Tooltip("Percent chance to add a room in a direction, once the minimum is reached.")]
        [SerializeField][Range(0, 100)] private int _baseGenerationChance;

        [Tooltip("Added to the chance after each room past the minimum.")]
        [SerializeField] private int _generationChanceDelta;

        [Header("Room Prefabs")]
        [SerializeField] private WeightedRoom[] _startRooms;

        [SerializeField] private WeightedRoom[] _combatRooms;

        [SerializeField] private WeightedRoom[] _endRooms;

        [Header("Debug (Do not edit)")]
        [SerializeField] private Room _startRoom;

        [SerializeField] private List<Room> _generatedCombatRooms;

        [SerializeField] private Room _endRoom;

        [SerializeField]
        private int _generationChance; // The current chance, which grows with each room past the minimum.

        /// <summary>Sets up the directions, the seed, and an empty grid. Generation waits for OnGenerateRequested.</summary>
        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }
            _directions = new Dictionary<Direction, Vector3>();
            _directions[Direction.North] = new Vector3(0, 0, 1);
            _directions[Direction.South] = new Vector3(0, 0, -1);
            _directions[Direction.East] = new Vector3(1, 0, 0);
            _directions[Direction.West] = new Vector3(-1, 0, 0);

            if (_generateSeed)
                _seed = Random.Range(-int.MaxValue, int.MaxValue);

            _random = new LinearCongruentialGenerator(_seed);

            _generationChance = _baseGenerationChance;

            _roomGrid = new Room[_roomGridHeight, _roomGridWidth];
            // INTEGRATION: removed navMeshSurface.BuildNavMesh(), which ran before any room existed. FinishGeneration bakes now.
            // INTEGRATION: removed "OnMapGenerated += SpawnPlayer". PlayerSpawner places the player now.
        }

        /// <summary>Listens for GameManager's generation request.</summary>
        private void OnEnable()
        {
            GameManager.OnGenerateRequested += ReceiveGeneration;
        }

        /// <summary>Stops listening for the generation request.</summary>
        private void OnDisable()
        {
            GameManager.OnGenerateRequested -= ReceiveGeneration;
        }

        // INTEGRATION: new. Rooms are created under this transform. When the NavMeshSurface collects only its children,
        // the rooms have to be its children to be baked. With Collect Objects set to All or Volume, rooms stay at the scene root.
        private Transform RoomParent =>
            _navMeshSurface != null && _navMeshSurface.collectObjects == CollectObjects.Children
                ? _navMeshSurface.transform
                : null;

        /// <summary>Checks the settings and builds the map. Runs when GameManager raises OnGenerateRequested.</summary>
        // Rocco: changed from generating on Start to waiting for GameManager's event.
        private void ReceiveGeneration()
        {
            if (!Validate())
            {
                Debug.LogError("[LevelGenerator] Error");
                return;
            }

            GenerateMap();
        }

        /// <summary>Debug: destroys the map and builds a new one, then moves the player to the new start room.</summary>
        // INTEGRATION: the I debug key became this Inspector button.
        [Button, DisableInEditorMode]
        private void DEBUGRegenerate()
        {
            ResetMap();
            GenerateMap();

            // INTEGRATION: PlayerSpawner only reacts to the first OnMapGenerated, so move the player here.
            GameObject player = GameManager.HasInstance ? GameManager.Instance.Player : null;
            PlayerSpawner spawner = player != null ? player.GetComponentInChildren<PlayerSpawner>() : null;
            if (spawner != null && _startRoom != null)
            {
                spawner.TeleportToRoom(_startRoom);
                _startRoom.EnterRoom();
            }
        }

        /// <summary>Debug: completes the start room and rolls its rewards.</summary>
        // INTEGRATION: the Left Ctrl debug key became this Inspector button.
        [Button, DisableInEditorMode]
        private void DEBUGCompleteStartRoom()
        {
            _startRoom.OnComplete(true);
        }

        /// <summary>
        /// Builds the map except for the End room: places a start room in the middle of the grid, then grows combat rooms
        /// outward, room by room, trying each direction in a random order. A room is added when the chance roll succeeds
        /// or the minimum isn't reached yet, and each new room links back to the room it grew from.
        /// </summary>
        private void GenerateMap()
        {
            Debug.Log($"Seed: {_seed}");
            LinearCongruentialGenerator roomRandom = new LinearCongruentialGenerator(_seed);

            Room startRoom = Instantiate(GetRandomStartRoom(roomRandom.Next()),
                gameObject.transform.position, Quaternion.identity, RoomParent); // INTEGRATION: created under RoomParent, so the NavMesh bake includes it.
            startRoom.Id = $"Start{(startRoom != null ? 1 : 0)}";

            _startRoom = startRoom;

            Vector2Int currentGridPos = new Vector2Int(_roomGridHeight / 2, _roomGridWidth / 2);
            _roomGrid[currentGridPos.x, currentGridPos.y] = startRoom;

            Queue<(Room, Vector2Int)> rooms = new Queue<(Room, Vector2Int)>();
            rooms.Enqueue((startRoom, currentGridPos));

            while (rooms.Count > 0 && _generatedCombatRooms.Count < _maxCombatRooms)
            {
                (Room room, Vector2Int currentPos) = rooms.Dequeue();

                // Shuffle the four directions by giving each a random priority.
                PriorityQueue<Direction> priorityQueue = new PriorityQueue<Direction>();

                foreach (Direction dir in Enum.GetValues(typeof(Direction)))
                    priorityQueue.Enqueue(dir, _random.Next(-int.MaxValue, int.MaxValue));

                while (priorityQueue.Count > 0)
                {
                    Vector2Int gridDiff = new Vector2Int(0, 0);
                    Direction direction = priorityQueue.Dequeue();

                    switch (direction)
                    {
                        case Direction.North:
                            gridDiff = new Vector2Int(-1, 0);
                            break;
                        case Direction.South:
                            gridDiff = new Vector2Int(1, 0);
                            break;
                        case Direction.East:
                            gridDiff = new Vector2Int(0, 1);
                            break;
                        case Direction.West:
                            gridDiff = new Vector2Int(0, -1);
                            break;
                    }

                    Vector2Int added = currentPos + gridDiff;

                    if (_generatedCombatRooms.Count >= _maxCombatRooms)
                        break;

                    if (added.x < 0 || added.y < 0 ||
                        added.x >= _roomGridHeight || added.y >= _roomGridWidth)
                    {
                        continue;
                    }

                    if (_random.Next(0, 100) < _generationChance || _generatedCombatRooms.Count < _minCombatRooms)
                    {
                        if (_roomGrid[added.x, added.y] != null)
                            continue;

                        Room selected = GetRandomCombatRoom(roomRandom.Next());
                        Room nextRoom = Instantiate(selected, room.transform.position, Quaternion.identity, RoomParent); // INTEGRATION: created under RoomParent for the bake.
                        // Spaced by the room's bounding box diagonal. Still to change to the room prefab standard's size (task 3).
                        nextRoom.transform.position = room.gameObject.transform.position + (_directions[direction] *
                            nextRoom.BoundingBox.bounds.size.magnitude);

                        nextRoom.Id = $"Combat{(nextRoom != null ? _generatedCombatRooms.Count : -1)}";

                        _roomGrid[added.x, added.y] = nextRoom;

                        // Link the two rooms as neighbors.
                        switch (direction)
                        {
                            case Direction.North:
                                room.North = nextRoom;
                                nextRoom.South = room;
                                break;
                            case Direction.South:
                                room.South = nextRoom;
                                nextRoom.North = room;
                                break;
                            case Direction.East:
                                room.East = nextRoom;
                                nextRoom.West = room;
                                break;
                            case Direction.West:
                                room.West = nextRoom;
                                nextRoom.East = room;
                                break;
                            default:
                                Debug.LogError("[LevelGenerator] Error: Invalid direction");
                                break;
                        }

                        // Each room has one portal, so a room that grows two children only leads to the last one (task 3 makes generation linear).
                        if (room.Portal != null)
                            room.Portal.Connected = nextRoom;

                        if (_generatedCombatRooms.Count >= _minCombatRooms)
                            _generationChance += _generationChanceDelta;

                        _generatedCombatRooms.Add(nextRoom);
                        rooms.Enqueue((nextRoom, added));
                    }
                }
            }

            PlaceEndRoom(roomRandom.Next());
        }

        /// <summary>
        /// Places the End room next to the room farthest from the start, on the first free side, links the two,
        /// and points the farthest room's portal at it. Then finishes generation.
        /// The End room isn't added to the grid yet, so it can overlap another room (task 3).
        /// </summary>
        private void PlaceEndRoom(long seed)
        {
            Room farthest = GetFarthest();

            if (farthest == null)
            {
                Debug.LogError("[LevelGenerator] Error: No farthest room found");
                return;
            }

            Direction direction = Direction.North;

            if (farthest.North == null)
                direction = Direction.North;
            else if (farthest.South == null)
                direction = Direction.South;
            else if (farthest.East == null)
                direction = Direction.East;
            else if (farthest.West == null)
                direction = Direction.West;

            Vector3 dir = _directions[direction];

            Room selected = GetRandomEndRoom(seed);
            Room end = Instantiate(selected, farthest.transform.position, Quaternion.identity, RoomParent);
            end.transform.position = farthest.gameObject.transform.position +
                                     (dir * end.BoundingBox.bounds.size.magnitude);

            end.Id = $"End{(end != null ? 1 : 0)}";

            switch (direction)
            {
                case Direction.North:
                    farthest.North = end;
                    end.South = farthest;
                    break;
                case Direction.South:
                    farthest.South = end;
                    end.North = farthest;
                    break;
                case Direction.East:
                    farthest.East = end;
                    end.West = farthest;
                    break;
                case Direction.West:
                    farthest.West = end;
                    end.East = farthest;
                    break;
            }

            _endRoom = end;
            farthest.Portal.Connected = _endRoom;

            FinishGeneration(); // INTEGRATION: was "OnMapGenerated?.Invoke()". The event now fires after the bake.
        }

        /// <summary>
        /// Runs once every room is placed:
        /// 1. Completes the start room if it has no RoomEncounter, which opens its portal. A start room with a
        ///    RoomEncounter has enemies and clears like a combat room instead.
        /// 2. Bakes the NavMesh over the placed rooms.
        /// 3. Fires OnMapGenerated, so PlayerSpawner places the player in a finished, baked map.
        /// </summary>
        private void FinishGeneration()
        {
            if (_startRoom != null && _startRoom.GetComponent<RoomEncounter>() == null)
                _startRoom.OnComplete();

            BakeNavMesh();

            OnMapGenerated?.Invoke(); // We are done generating the map
        }

        /// <summary>Bakes the NavMesh over the placed rooms. Enemies can only move once this has run.</summary>
        // INTEGRATION: new. Replaces the bake in Awake, which ran before any room existed, so enemies had no NavMesh.
        private void BakeNavMesh()
        {
            if (_navMeshSurface == null)
            {
                Debug.LogWarning("[LevelGenerator] No NavMeshSurface assigned. Enemies won't be able to move.", this);
                return;
            }

            Physics.SyncTransforms(); // The rooms were moved this frame, so update their colliders before baking.
            _navMeshSurface.BuildNavMesh();
        }

        /// <summary>
        /// Finds the room farthest from the start by walking the neighbor links breadth first. The last room visited is the farthest.
        /// </summary>
        private Room GetFarthest()
        {
            Queue<Room> rooms = new Queue<Room>();
            HashSet<Room> visited = new HashSet<Room>();

            rooms.Enqueue(_startRoom);
            Room last = null;

            while (rooms.Count > 0)
            {
                Room room = rooms.Dequeue();
                last = room;
                visited.Add(room);

                if (room.North != null && !visited.Contains(room.North))
                    rooms.Enqueue(room.North);
                if (room.South != null && !visited.Contains(room.South))
                    rooms.Enqueue(room.South);
                if (room.East != null && !visited.Contains(room.East))
                    rooms.Enqueue(room.East);
                if (room.West != null && !visited.Contains(room.West))
                    rooms.Enqueue(room.West);
            }

            return last;
        }

        /// <summary>Destroys every room and resets the grid, the chance, and the seed.</summary>
        private void ResetMap()
        {
            Room[] rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);

            foreach (Room room in rooms)
            {
                room.gameObject.SetActive(false); // INTEGRATION: Destroy waits until the end of the frame, and the bake skips inactive objects.
                Destroy(room.gameObject);
            }

            _startRoom = null;
            _endRoom = null;
            _generatedCombatRooms.Clear();
            _generationChance = _baseGenerationChance;
            _roomGrid = new Room[_roomGridHeight, _roomGridWidth];

            if (_generateSeed)
                _seed = Random.Range(-int.MaxValue, int.MaxValue);

            _random = new LinearCongruentialGenerator(_seed);
        }

        /// <summary>Returns false if the settings can't build a map: the minimum is over the maximum, or a room list is empty.</summary>
        private bool Validate()
        {
            if (_maxCombatRooms < _minCombatRooms)
                return false;

            if (_startRooms.Length == 0)
                return false;

            if (_endRooms.Length == 0)
                return false;

            if (_combatRooms.Length == 0)
                return false;

            return true;
        }

        /// <summary>Picks a combat room prefab by weight.</summary>
        private Room GetRandomCombatRoom(long seed)
        {
            ItemSelector<WeightedRoom> roomSelector = new ItemSelector<WeightedRoom>(seed);

            foreach (WeightedRoom room in _combatRooms)
                roomSelector.AddItem(room, room.Weight);

            return roomSelector.Roll().Room;
        }

        /// <summary>Picks a start room prefab by weight.</summary>
        private Room GetRandomStartRoom(long seed)
        {
            ItemSelector<WeightedRoom> roomSelector = new ItemSelector<WeightedRoom>(seed);

            foreach (WeightedRoom room in _startRooms)
                roomSelector.AddItem(room, room.Weight);

            return roomSelector.Roll().Room;
        }

        /// <summary>Picks an End room prefab by weight.</summary>
        private Room GetRandomEndRoom(long seed)
        {
            ItemSelector<WeightedRoom> roomSelector = new ItemSelector<WeightedRoom>(seed);

            foreach (WeightedRoom room in _endRooms)
                roomSelector.AddItem(room, room.Weight);

            return roomSelector.Roll().Room;
        }

        // INTEGRATION: removed the temporary player field and SpawnPlayer, which used GameObject.Find("Player").
        // PlayerSpawner.SpawnWhenMapReady places the player now.
    }
}