using System;
using System.Collections.Generic;
using UnityEngine;

public class GunBody : MonoBehaviour
{
    private Dictionary<GunPartType, GunSocket> sockets = new();

    private void Awake()
    {
        GunSocket[] sockets = GetComponentsInChildren<GunSocket>(true);

        foreach (GunSocket socket in sockets)
        {
        }
    }
}
