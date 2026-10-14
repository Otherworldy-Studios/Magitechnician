using System;
using System.Collections.Generic;
using _Project.Scripts.LevelGenerator.Utility;
using _Project.Scripts.LevelGenerator.Utility.System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace _Project.Scripts.LevelGenerator
{
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
        private enum Direction
        {
            North,
            South,
            East,
            West
        }

        private LinearCongruentialGenerator _random;
        private Dictionary<Direction, Vector3> _directions;
        private Room[,] _roomGrid;

        [Header("Generator Settings")]
        [SerializeField] private int _roomGridSize;

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
        [SerializeField] private Room _startRoom;

        [SerializeField] private List<Room> _generatedCombatRooms;

        [SerializeField] private Room _endRoom;
        
        [SerializeField]
        private int _generationChance;

        void Awake()
        {
            _directions = new Dictionary<Direction, Vector3>();
            _directions[Direction.North] = new Vector3(0, 0, 1);
            _directions[Direction.South] = new Vector3(0, 0, -1);
            _directions[Direction.East] = new Vector3(1, 0, 0);
            _directions[Direction.West] = new Vector3(-1, 0, 0);

            if (_generateSeed)
                _seed = Random.Range(-int.MaxValue, int.MaxValue);

            _random = new LinearCongruentialGenerator(_seed);

            _generationChance = _baseGenerationChance;
            
            _roomGrid = new Room[_roomGridSize, _roomGridSize];
        }

        void Start()
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
            }
            #endif
        }

        private void GenerateMap()
        {
            Debug.Log($"Seed: {_seed}");
            
            Room startRoom = Instantiate(GetRandomStartRoom(), 
                gameObject.transform.position, Quaternion.identity);
            
            _startRoom = startRoom;
            
            Vector2Int currentGridPos = new Vector2Int(_roomGridSize / 2, _roomGridSize / 2);
            _roomGrid[currentGridPos.x, currentGridPos.y] = startRoom;

            Queue<(Room, Vector2Int)> rooms = new Queue<(Room, Vector2Int)>();
            rooms.Enqueue((startRoom, currentGridPos));

            while (rooms.Count > 0 && _generatedCombatRooms.Count < _maxCombatRooms)
            {
                (Room room, Vector2Int currentPos) = rooms.Dequeue();
                
                PriorityQueue<Direction> priorityQueue = new PriorityQueue<Direction>();
                
                foreach(Direction dir in Enum.GetValues(typeof(Direction)))
                    priorityQueue.Enqueue(dir, _random.Next());
                
                while(priorityQueue.Count > 0)
                {
                    Direction direction = priorityQueue.Dequeue();
                    if (_generatedCombatRooms.Count >= _maxCombatRooms)
                        break;

                    Vector2Int gridDiff = new Vector2Int(0, 0);

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

                    if (added.x < 0 || added.y < 0 ||
                        added.x >= _roomGridSize || added.y >= _roomGridSize)
                    {
                        continue;
                    }

                    if (_random.Next(0, 100) < _generationChance || _generatedCombatRooms.Count < _minCombatRooms)
                    {
                        if (_roomGrid[added.x, added.y] != null)
                            continue;

                        Room nextRoom = Instantiate(GetRandomCombatRoom(), 
                            room.gameObject.transform.position + (_directions[direction] * 2), 
                            Quaternion.identity);
                        
                        _roomGrid[added.x, added.y] = nextRoom;

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
        }

        private void ResetMap()
        {
            Room[] rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
            
            foreach (Room room in rooms)
                Destroy(room.gameObject);

            _startRoom = null;
            _endRoom = null;
            _generatedCombatRooms.Clear();
            _generationChance = _baseGenerationChance;
            
            if (_generateSeed)
                _seed = Random.Range(-int.MaxValue, int.MaxValue);
        }

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

        private Room GetRandomCombatRoom()
        {
            ItemSelector<WeightedRoom> roomSelector = new ItemSelector<WeightedRoom>();
            
            foreach (WeightedRoom room in _combatRooms)
                roomSelector.AddItem(room, room.Weight);
            
            return roomSelector.Roll().Room;
        }

        private Room GetRandomStartRoom()
        {
            ItemSelector<WeightedRoom> roomSelector = new ItemSelector<WeightedRoom>();
            
            foreach (WeightedRoom room in _startRooms)
                roomSelector.AddItem(room, room.Weight);
            
            return roomSelector.Roll().Room;
        }

        private Room GetRandomEndRoom()
        {
            ItemSelector<WeightedRoom> roomSelector = new ItemSelector<WeightedRoom>();
            
            foreach (WeightedRoom room in _endRooms)
                roomSelector.AddItem(room, room.Weight);
            
            return roomSelector.Roll().Room;
        }
    }
}