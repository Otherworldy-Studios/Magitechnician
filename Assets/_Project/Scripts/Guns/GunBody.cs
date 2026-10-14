using System.Collections.Generic;
using UnityEngine;

public class GunBody : MonoBehaviour
{
    private Dictionary<GunPartType, GunSocket> _sockets = new();

    private void Awake()
    {
        GunSocket[] sockets = GetComponentsInChildren<GunSocket>(true);

        foreach (GunSocket socket in sockets)
        {
        }
    }

    public bool GetSocket(GunPartType type, out GunSocket socket)
    {
        return _sockets.TryGetValue(type, out socket);
    }
}
