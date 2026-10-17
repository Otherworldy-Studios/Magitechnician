using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GunBody))]
public class Gun : MonoBehaviour
{
    [SerializeField] private GunFrame _defaultFrame;
    [SerializeField] private GunPart[] _gunParts;

    private Dictionary<GunPartType, GunPart> _gunSockets;
    private Dictionary<GunPartType, GunPart> _equippedParts;

    public bool HasSocket(GunPartType type)
    {
        return true;
    }

    public GunPart GetEquippedPart(GunPartType type)
    {
        return null;
    }

    public bool EquipPart(GunPart part)
    {
        return true;
    }
    
    public bool RemovePart(GunPartType type)
    {
        return true;
    }
    
    private void SpawnPartModel(GunPart part)
    {}
    
    private void RemovePartModel(GunPart part)
    {}
}
