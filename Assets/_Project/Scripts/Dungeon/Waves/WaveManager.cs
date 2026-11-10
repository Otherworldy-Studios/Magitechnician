using _Project.Scripts.Dungeon;
using UnityEngine;

public class WaveManager : MGR<WaveManager>
{
    [SerializeField] 
    private int _currentWave;
    
    override protected void Awake()
    {
        base.Awake();
        LevelGenerator.Instance.OnMapGenerated += SubscribeToRoomEvents;
        _currentWave = 0;
    }

    void SubscribeToRoomEvents()
    {
        foreach (Room room in LevelGenerator.Instance.RoomGrid)
        {
            if (room != null && room.Type != Room.RoomType.Start)
                room.OnRoomEntered += StartWave;
        }
    }

    void StartWave(Room room)
    {
        
    }
}
