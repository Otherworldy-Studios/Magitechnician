using System;
using System.Collections.Generic;
using _Project.Scripts.Dungeon.Utility;
using _Project.Scripts.Dungeon.Utility.System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Dungeon
{
    /// <summary>
    /// Represents a room and the weight it will hold in the
    /// level generator's weighted randomizer
    /// </summary>
    [Serializable]
    struct WeightedRoom
    {
        [SerializeField] private Room _room;
        
        [SerializeField] private int _weight;
        
        public Room Room { get { return _room; } }
        public int Weight { get { return _weight; } }
    }
    
    public class LevelGenerator : MonoBehaviour
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

        private LinearCongruentialGenerator _random;
        private Dictionary<Direction, Vector3> _directions;
        private Room[,] _roomGrid;
        public event Action OnMapGenerated; // This is for the game manager to know when the map is generated

        // The 2D array representing the map layout
        public Room[,] RoomGrid  { get  { return _roomGrid; } }
        public Room StartRoom => _startRoom;

        [Header("Generator Settings")] 
        [SerializeField] private int _roomOffset; //this should not be hard-coded - Lee
        
        [SerializeField] private int _roomGridWidth;
        [SerializeField] private int _roomGridHeight;

        [SerializeField] private int _minCombatRooms;
        
        [SerializeField] private int _maxCombatRooms;

        [SerializeField] private int _seed;

        [SerializeField] private bool _generateSeed;

        [SerializeField] [Range(0, 100)] private int _baseGenerationChance;

        [SerializeField] private int _generationChanceDelta;

        [Header("Room Prefabs")] 
        [SerializeField] private WeightedRoom[] _startRooms;

        [SerializeField] private WeightedRoom[] _combatRooms;

        [SerializeField] private WeightedRoom[] _endRooms;

        [Header("Debug (Do not edit)")] 
        [SerializeField] public Room _startRoom;

        [SerializeField] private List<Room> _generatedCombatRooms;

        [SerializeField] private Room _endRoom;
        
        [SerializeField]
        private int _generationChance;


        public static LevelGenerator Instance;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
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
            GameManager.OnGenerateRequested += ReceiveGeneration;
        }

        private void OnDisable()
        {
            GameManager.OnGenerateRequested -= ReceiveGeneration;
        }

        private void ReceiveGeneration() //rocco: changed it generating on start and now relies on GM event
        {
            if (!Validate())
            {
                Debug.LogError("[LevelGenerator] Error");
                return;
            }

            GenerateMap();
        }

        void Update()
        {
            #if UNITY_EDITOR
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    ResetMap();
                    GenerateMap();
                }
                
                if (Keyboard.current.leftCtrlKey.wasPressedThisFrame)
                {
                    Debug.Log("Rolling debug");
                    _startRoom.OnComplete(true);
                }
            }
            #endif
        }

        /// <summary>
        /// Generates the entire map except for the end room
        /// </summary>
        private void GenerateMap()
        {
            Debug.Log($"Seed: {_seed}");
            
            Room startRoom = Instantiate(GetRandomStartRoom(), 
                gameObject.transform.position, Quaternion.identity);
            startRoom.Id = $"Start{(startRoom != null ? 1 : 0)}";
            
            _startRoom = startRoom;
            
            Vector2Int currentGridPos = new Vector2Int(_roomGridHeight / 2, _roomGridWidth / 2);
            _roomGrid[currentGridPos.x, currentGridPos.y] = startRoom;

            Queue<(Room, Vector2Int)> rooms = new Queue<(Room, Vector2Int)>();
            rooms.Enqueue((startRoom, currentGridPos));

            while (rooms.Count > 0 && _generatedCombatRooms.Count < _maxCombatRooms)
            {
                (Room room, Vector2Int currentPos) = rooms.Dequeue();
                
                // This randomizes the directions we will generate in
                PriorityQueue<Direction> priorityQueue = new PriorityQueue<Direction>();
                
                foreach(Direction dir in Enum.GetValues(typeof(Direction)))
                    priorityQueue.Enqueue(dir, _random.Next());
                
                while(priorityQueue.Count > 0)
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
                        
                        Room nextRoom = Instantiate(GetRandomCombatRoom(),
                            room.gameObject.transform.position + (_directions[direction] * _roomOffset),
                            Quaternion.identity);
                        nextRoom.Id = $"Combat{(nextRoom != null ? _generatedCombatRooms.Count : -1)}";

                        _roomGrid[added.x, added.y] = nextRoom;

                        // Linking rooms
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
                        
                        if(_generatedCombatRooms.Count >= _minCombatRooms)
                            _generationChance += _generationChanceDelta;
                        
                        _generatedCombatRooms.Add(nextRoom);
                        rooms.Enqueue((nextRoom, added));
                    }
                }
            }
            
            PlaceEndRoom();
        }

        /// <summary>
        /// Places the ending boss room
        /// </summary>
        private void PlaceEndRoom()
        {
            Room farthest = GetFarthest();

            if (farthest == null)
                return;

            Direction direction = Direction.North;

            if (farthest.North == null)
                direction = Direction.North;
            else if (farthest.South == null)
                direction = Direction.South;
            else if (farthest.East == null)
                direction = Direction.East;
            else if(farthest.West == null)
                direction = Direction.West;
            
            Vector3 dir = _directions[direction];
            
            Room end = Instantiate(GetRandomEndRoom(),
                farthest.gameObject.transform.position + (dir * _roomOffset),
                Quaternion.identity);
            
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
            OnMapGenerated?.Invoke(); // We are done generating the map
        }

        /// <summary>
        /// Finds the "farthest" room using
        /// BFS
        /// </summary>
        /// <returns>
        /// The farthest room
        /// </returns>
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
                if(room.South != null && !visited.Contains(room.South))
                    rooms.Enqueue(room.South);
                if (room.East != null && !visited.Contains(room.East))
                    rooms.Enqueue(room.East);
                if(room.West != null && !visited.Contains(room.West))
                    rooms.Enqueue(room.West);
            }

            return last;
        }

        /// <summary>
        /// Wipes the entire map
        /// </summary>
        private void ResetMap()
        {
            Room[] rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
            
            foreach (Room room in rooms)
                Destroy(room.gameObject);

            _startRoom = null;
            _endRoom = null;
            _generatedCombatRooms.Clear();
            _generationChance = _baseGenerationChance;
            _roomGrid = new Room[_roomGridHeight, _roomGridWidth];
            
            if (_generateSeed)
                _seed = Random.Range(-int.MaxValue, int.MaxValue);

            _random = new LinearCongruentialGenerator(_seed);
        }

        /// <summary>
        /// Basic validation checks to make sure
        ///  the generator has valid information
        /// </summary>
        /// <returns></returns>
        private bool Validate()
        {
            if (_maxCombatRooms < _minCombatRooms)
                return false;
            
            if(_startRooms.Length == 0)
                return false;
                
            if (_endRooms.Length == 0)
                return false;
                
            if (_combatRooms.Length == 0)
                return false;
                
            return true;
        }

        /// <summary>
        /// Gets a random room from the room pool
        /// </summary>
        /// <returns>
        /// A random room
        /// </returns>
        private Room GetRandomCombatRoom()
        {
            ItemSelector<WeightedRoom> roomSelector = new ItemSelector<WeightedRoom>();
            
            foreach (WeightedRoom room in _combatRooms)
                roomSelector.AddItem(room, room.Weight);
            
            return roomSelector.Roll().Room;
        }

        /// <summary>
        /// Gets a random start room (if we have more than one)
        /// </summary>
        /// <returns>
        /// A random start room
        /// </returns>
        private Room GetRandomStartRoom()
        {
            ItemSelector<WeightedRoom> roomSelector = new ItemSelector<WeightedRoom>();
            
            foreach (WeightedRoom room in _startRooms)
                roomSelector.AddItem(room, room.Weight);
            
            return roomSelector.Roll().Room;
        }

        /// <summary>
        /// Gets a random end room (if we have more than one)
        /// </summary>
        /// <returns>
        /// A random end room
        /// </returns>
        private Room GetRandomEndRoom()
        {
            ItemSelector<WeightedRoom> roomSelector = new ItemSelector<WeightedRoom>();
            
            foreach (WeightedRoom room in _endRooms)
                roomSelector.AddItem(room, room.Weight);
            
            return roomSelector.Roll().Room;
        }
    }
}
