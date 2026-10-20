using UnityEngine;

namespace _Project.Scripts.Dungeon
{
    public class Room : MonoBehaviour
    {
        public enum RoomType
        {
            Start,
            Combat,
            End
        }

        [Header("Room Settings")] 
        [SerializeField]
        private RoomType roomType;

        [Header("Debug (Do not edit)")] 
        [SerializeField] 
        private Room _north;

        public Room North
        {
            get => _north;
            set => _north = value;
        }

        [SerializeField] private Room _south;

        public Room South
        {
            get => _south;
            set => _south = value;
        }

        [SerializeField] private Room _east;

        public Room East
        {
            get => _east;
            set => _east = value;
        }

        [SerializeField] private Room _west;

        public Room West
        {
            get => _west;
            set => _west = value;
        }

        void Start()
        {

        }

        void Update()
        {

        }
    }
}
