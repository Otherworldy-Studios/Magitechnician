using _Project.Scripts.Dungeon.Utility;
using _Project.Scripts.Dungeon.Utility.System.Collections.Generic;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
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

        public Room Room => _room;
        public int Weight => _weight;
    }

    /// <summary>
    /// Builds the dungeon in the Level scene when GameManager raises OnGenerateRequested: places a start room, grows
    /// combat rooms out from it on a grid using a seed, links neighbors and portals, puts the End room next to the
    /// farthest room, bakes the NavMesh, and fires OnMapGenerated.
    /// PlayerSpawner and GameManager start the run from that event.
    /// </summary>
    // Inherits MGR<LevelGenerator> with Should Persist off, so it only exists in the Level scene.
    public class LevelGenerator : MGR<LevelGenerator>
    {
        public enum Direction
        {
            North,
            South,
            East,
            West
        }

        // Set Use Geometry to Physics Colliders, because baking Render Meshes in a build needs Read/Write on every model.
        [Tooltip("The surface to bake once the rooms are placed.")]
        [SerializeField] private NavMeshSurface _navMeshSurface;

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
        [SerializeField, Range(0, 100)] private int _baseGenerationChance;

        [Tooltip("Added to the chance after each room past the minimum.")]
        [SerializeField] private int _generationChanceDelta;

        [Tooltip("Meters of empty space between the bounding boxes of two rooms next to each other.")]
        [SerializeField, Min(0f)] private float _roomGap = 5f;

        [Header("Room Prefabs")]
        [SerializeField] private WeightedRoom[] _startRooms;
        [SerializeField] private WeightedRoom[] _combatRooms;
        [SerializeField] private WeightedRoom[] _endRooms;

        [Header("Debug (Do not edit)"), Space(5)]
        [SerializeField, ReadOnly] private Room _startRoom;
        [SerializeField, ReadOnly] private List<Room> _generatedCombatRooms;
        [SerializeField, ReadOnly] private Room _endRoom;
        [Tooltip("Grows with each room past the minimum.")]
        [SerializeField, ReadOnly] private int _generationChance;

        [ReadOnly]
        public bool GenerationComplete;

        private LinearCongruentialGenerator _random;          // Seeded random numbers for the generation chances and direction order.
        private Dictionary<Direction, Vector3> _directions;   // World direction for each grid direction.
        private Room[,] _roomGrid;                             // Each cell holds a room or null.

        /// <summary>
        /// Fires after the map is built and the NavMesh is baked.
        /// PlayerSpawner places the player from it, and that starts the run.
        /// </summary>
        public event Action OnMapGenerated;

        /// <summary>The map layout. Each cell holds a room or null. GameManager copies it when the run starts.</summary>
        public Room[,] RoomGrid => _roomGrid;
        public Room StartRoom => _startRoom;

        // Rooms are created under this transform. When the NavMeshSurface collects only its children, the rooms have to
        // be its children to be baked. With Collect Objects set to All or Volume, rooms stay at the scene root.
        private Transform RoomParent =>
            _navMeshSurface != null && _navMeshSurface.collectObjects == CollectObjects.Children
                ? _navMeshSurface.transform
                : null;

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
        }

        private void OnEnable()
        {
            if (!GenerationComplete)
            {
                GameManager.OnGenerateRequested += ReceiveGeneration;
            }
        }

        private void OnDisable()
        {
            GameManager.OnGenerateRequested -= ReceiveGeneration;
        }

        /// <summary>Checks the settings and builds the map. Runs when GameManager raises OnGenerateRequested.</summary>
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
        [Button, DisableInEditorMode]
        private void DEBUGRegenerate()
        {
            ResetMap();
            GenerateMap();

            // PlayerSpawner only reacts to the first OnMapGenerated, so move the player here.
            GameObject player = GameManager.HasInstance ? GameManager.Instance.Player : null;
            PlayerSpawner spawner = player != null ? player.GetComponentInChildren<PlayerSpawner>() : null;
            if (spawner != null && _startRoom != null)
            {
                spawner.TeleportToRoom(_startRoom);
                _startRoom.EnterRoom();
            }
        }

        /// <summary>Debug: completes the start room, which opens its portal.</summary>
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
            GenerationComplete = false;
            LinearCongruentialGenerator roomRandom = new LinearCongruentialGenerator(_seed);

            // Created under RoomParent, so the NavMesh bake includes it.
            Room startRoom = Instantiate(GetRandomStartRoom(roomRandom.Next()),
                gameObject.transform.position, Quaternion.identity, RoomParent);
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
                        Room nextRoom = Instantiate(selected, room.transform.position, Quaternion.identity, RoomParent);
                        nextRoom.transform.position = PositionNextTo(room, nextRoom, direction);

                        // Rooms can differ in size, so a room grown from one neighbor can still reach into another.
                        if (OverlapsPlacedRoom(nextRoom, room))
                        {
                            nextRoom.gameObject.SetActive(false); // Destroy waits until the end of the frame, and the bake skips inactive objects.
                            Destroy(nextRoom.gameObject);
                            continue;
                        }

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
        /// Places the End room next to the room farthest from the start, on the first side that's free and where it
        /// doesn't overlap another room, links the two, and points the farthest room's portal at it. When no side
        /// fits, such as for a large boss room, places it past every other room instead. The portal teleports the
        /// player, so the End room doesn't have to touch the farthest room. Then finishes generation.
        /// </summary>
        private void PlaceEndRoom(long seed)
        {
            Room farthest = GetFarthest();

            if (farthest == null)
            {
                Debug.LogError("[LevelGenerator] Error: No farthest room found");
                return;
            }

            Room selected = GetRandomEndRoom(seed);
            Room end = Instantiate(selected, farthest.transform.position, Quaternion.identity, RoomParent);
            end.Id = "End1";

            bool placed = false;
            Direction direction = Direction.North;

            foreach (Direction side in new[] { Direction.North, Direction.South, Direction.East, Direction.West })
            {
                if (GetNeighbor(farthest, side) != null)
                    continue;

                end.transform.position = PositionNextTo(farthest, end, side);

                if (OverlapsPlacedRoom(end, farthest))
                    continue;

                direction = side;
                placed = true;
                break;
            }

            if (!placed)
            {
                end.transform.position = PositionPastAllRooms(end);

                // The link only needs a free side on the farthest room. With none free, the portal alone connects them.
                foreach (Direction side in new[] { Direction.North, Direction.South, Direction.East, Direction.West })
                {
                    if (GetNeighbor(farthest, side) != null)
                        continue;

                    direction = side;
                    placed = true;
                    break;
                }
            }

            if (placed)
                LinkNeighbors(farthest, end, direction);

            _endRoom = end;
            farthest.Portal.Connected = _endRoom;

            FinishGeneration();
        }

        private static void LinkNeighbors(Room from, Room to, Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    from.North = to;
                    to.South = from;
                    break;
                case Direction.South:
                    from.South = to;
                    to.North = from;
                    break;
                case Direction.East:
                    from.East = to;
                    to.West = from;
                    break;
                case Direction.West:
                    from.West = to;
                    to.East = from;
                    break;
            }
        }

        /// <summary>
        /// Runs once every room is placed. Bakes the NavMesh over the placed rooms, then fires OnMapGenerated, so
        /// PlayerSpawner places the player in a finished, baked map. The start room isn't completed here, because it
        /// has enemies and clears through WaveManager like a combat room.
        /// </summary>
        private void FinishGeneration()
        {
            BakeNavMesh();
            GenerationComplete = true;
            OnMapGenerated?.Invoke();
        }

        /// <summary>
        /// Where to put a room so its bounding box sits Room Gap past the other room's box in a direction, with the two
        /// boxes' centers lined up on the other axis and the rooms' origins at the same height. Works for rooms of any size.
        /// </summary>
        private Vector3 PositionNextTo(Room from, Room to, Direction direction)
        {
            Vector3 axis = _directions[direction];
            GetFootprint(from, out Vector3 fromCenter, out Vector3 fromHalfSize);
            GetFootprint(to, out Vector3 toCenter, out Vector3 toHalfSize);

            float spacing = Vector3.Dot(fromHalfSize, Abs(axis)) + Vector3.Dot(toHalfSize, Abs(axis)) + _roomGap;
            Vector3 toCenterOffset = toCenter - to.transform.position;

            Vector3 position = fromCenter + axis * spacing - toCenterOffset;
            position.y = from.transform.position.y; // Keeps the floors level when the boxes have different heights.
            return position;
        }

        /// <summary>A position east of every placed room, Room Gap past the farthest east edge, level with the start room.</summary>
        private Vector3 PositionPastAllRooms(Room room)
        {
            float eastEdge = float.MinValue;

            foreach (Room placed in PlacedRooms())
            {
                GetFootprint(placed, out Vector3 placedCenter, out Vector3 placedHalfSize);
                eastEdge = Mathf.Max(eastEdge, placedCenter.x + placedHalfSize.x);
            }

            GetFootprint(room, out Vector3 roomCenter, out Vector3 roomHalfSize);
            Vector3 centerOffset = roomCenter - room.transform.position;

            Vector3 center = new Vector3(eastEdge + _roomGap + roomHalfSize.x, _startRoom.transform.position.y, _startRoom.transform.position.z);
            return center - new Vector3(centerOffset.x, 0f, centerOffset.z);
        }

        /// <summary>True when the room's bounding box, plus half of Room Gap, overlaps any placed room's box other than the one given.</summary>
        private bool OverlapsPlacedRoom(Room room, Room except)
        {
            GetFootprint(room, out Vector3 center, out Vector3 halfSize);
            Bounds bounds = new Bounds(center, halfSize * 2f + Vector3.one * _roomGap * 0.5f);

            foreach (Room placed in PlacedRooms())
            {
                if (placed == room || placed == except)
                    continue;

                GetFootprint(placed, out Vector3 placedCenter, out Vector3 placedHalfSize);

                if (bounds.Intersects(new Bounds(placedCenter, placedHalfSize * 2f)))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// The room's bounding box in world space, worked out from the BoxCollider's center and size instead of its
        /// bounds, because a room moved this frame still reports its old bounds until the physics scene updates.
        /// </summary>
        private static void GetFootprint(Room room, out Vector3 center, out Vector3 halfSize)
        {
            BoxCollider box = room.BoundingBox;

            if (box == null)
            {
                center = room.transform.position;
                halfSize = Vector3.zero;
                return;
            }

            Vector3 scale = box.transform.lossyScale;
            center = box.transform.TransformPoint(box.center);
            halfSize = Vector3.Scale(box.size * 0.5f, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        }

        private IEnumerable<Room> PlacedRooms()
        {
            if (_startRoom != null)
                yield return _startRoom;

            foreach (Room room in _generatedCombatRooms)
            {
                if (room != null)
                    yield return room;
            }
        }

        private static Room GetNeighbor(Room room, Direction direction)
        {
            return direction switch
            {
                Direction.North => room.North,
                Direction.South => room.South,
                Direction.East => room.East,
                _ => room.West
            };
        }

        private static Vector3 Abs(Vector3 vector)
        {
            return new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
        }

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
                room.gameObject.SetActive(false); // Destroy waits until the end of the frame, and the bake skips inactive objects.
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

        /// <summary>Returns false if the minimum is over the maximum, or a room list is empty.</summary>
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

        private Room GetRandomCombatRoom(long seed)
        {
            ItemSelector<WeightedRoom> roomSelector = new ItemSelector<WeightedRoom>(seed);

            foreach (WeightedRoom room in _combatRooms)
                roomSelector.AddItem(room, room.Weight);

            return roomSelector.Roll().Room;
        }

        private Room GetRandomStartRoom(long seed)
        {
            ItemSelector<WeightedRoom> roomSelector = new ItemSelector<WeightedRoom>(seed);

            foreach (WeightedRoom room in _startRooms)
                roomSelector.AddItem(room, room.Weight);

            return roomSelector.Roll().Room;
        }

        private Room GetRandomEndRoom(long seed)
        {
            ItemSelector<WeightedRoom> roomSelector = new ItemSelector<WeightedRoom>(seed);

            foreach (WeightedRoom room in _endRooms)
                roomSelector.AddItem(room, room.Weight);

            return roomSelector.Roll().Room;
        }
    }
}