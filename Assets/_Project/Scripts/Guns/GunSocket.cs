using UnityEngine;

public class GunSocket : MonoBehaviour
{
    [SerializeField] private GunPartType _socketType;

    public GunPartType SocketType => _socketType;
}
