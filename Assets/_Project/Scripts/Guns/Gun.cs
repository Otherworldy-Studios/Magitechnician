using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GunBody))]
public class Gun : MonoBehaviour
{
    // [SerializeField] private GunStats _gunStats;

    private GunBody _gunBody;
    private Dictionary<GunPartType, GameObject> _partModels = new();
    private Dictionary<GunPartType, GunPartData> _equippedParts = new();

    private void Awake()
    {
        _gunBody = GetComponent<GunBody>();
    }

    public bool EquipPart(GunPartData part)
    {
        if (!_gunBody.GetSocket(part.PartType, out GunSocket socket))
        {
            return false;
        }
        
        GameObject newModel = Instantiate(part.PartPrefab, socket.transform, false);

        if (_partModels.TryGetValue(part.PartType, out GameObject oldModel))
        {
            Destroy(oldModel);
        }

        _partModels[part.PartType] = newModel;
        _equippedParts[part.PartType] = part;

        return true;
    }

    public bool RemovePart(GunPartData part)
    {
        return true;
    }
}
