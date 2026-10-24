using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GunBody))]
public class Gun : MonoBehaviour
{
    [SerializeField] private GunFrame _defaultFrame;
    [SerializeField] private GunPart[] _gunParts;

    private GunBody _gunBody;
    private Dictionary<GunPartType, GunSocket> _gunSockets;
    private Dictionary<GunPartType, GameObject> _partModels;
    private Dictionary<GunPartType, GunPart> _equippedParts;

    private void Awake()
    {
        _gunBody = GetComponent<GunBody>();

        _gunSockets = new Dictionary<GunPartType, GunSocket>();
        _partModels = new Dictionary<GunPartType, GameObject>();
        _equippedParts = new Dictionary<GunPartType, GunPart>();
        
        BuildGun();
    }

    // Builds/Rebuilds the gun using the set default frame and initial parts
    public void BuildGun()
    {
        ClearGun();

        if (!_defaultFrame)
        {
            return;
        }

        _gunBody.SetFrame(_defaultFrame);
        _equippedParts[GunPartType.Frame] = _defaultFrame;

        // Only allows one socket per type to be registered
        foreach (GunSocket socket in _gunBody.GetComponentsInChildren<GunSocket>(true))
        {
            if (_gunBody.GetSocket(socket.SocketType, out GunSocket activeSocket) && activeSocket == socket)
            {
                _gunSockets.TryAdd(socket.SocketType, socket);
            }
        }

        if (_gunParts != null)
        {
            foreach (GunPart part in _gunParts)
            {
                if (!part)
                {
                    continue;
                }

                EquipPart(part);
            }
        }
    }

    public bool HasSocket(GunPartType type)
    {
        return _gunSockets.ContainsKey(type);
    }

    public GunPart GetEquippedPart(GunPartType type)
    {
        _equippedParts.TryGetValue(type, out GunPart part);
        
        return part;
    }

    // Equips a part if the socket exists
    public bool EquipPart(GunPart part)
    {
        if (!part)
        {
            return false;
        }

        if (!HasSocket(part.PartType))
        {
            return false;
        }

        if (_equippedParts.ContainsKey(part.PartType))
        {
            RemovePart(part.PartType);
        }

        _equippedParts[part.PartType] = part;
        
        SpawnPartModel(part);
        
        // ToDo: Put Recalculate Stats method here using equipped parts
        
        return true;
    }
    
    public bool RemovePart(GunPartType type)
    {
        if (!_equippedParts.TryGetValue(type, out GunPart part))
        {
            return false;
        }
        
        RemovePartModel(part);

        _equippedParts.Remove(type);
        
        // ToDo: Put Recalculate Stats method here using equipped parts
        
        return true;
    }

    private void SpawnPartModel(GunPart part)
    {
        RemovePartModel(part);

        if (!part.PartPrefab)
        {
            return;
        }

        if (!_gunSockets.TryGetValue(part.PartType, out GunSocket socket))
        {
            return;
        }

        // Spawns the part at the socket then resets its position and rotation
        GameObject model = Instantiate(part.PartPrefab, socket.transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        _partModels[part.PartType] = model;
    }

    private void RemovePartModel(GunPart part)
    {
        if (!_partModels.TryGetValue(part.PartType, out GameObject model))
        {
            return;
        }

        if (model)
        {
            Destroy(model);
        }

        _partModels.Remove(part.PartType);
    }

    // Clears the guns parts and sockets
    private void ClearGun()
    {
        foreach (GameObject model in _partModels.Values)
        {
            if (model)
            {
                Destroy(model);
            }
        }
        
        _gunSockets.Clear();
        _partModels.Clear();
        _equippedParts.Clear();
    }
}
