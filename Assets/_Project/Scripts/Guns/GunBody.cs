using System.Collections.Generic;
using UnityEngine;

public class GunBody : MonoBehaviour
{
    private GameObject _frameModel;
    private Dictionary<GunPartType, GunSocket> _sockets = new();

    public void SetFrame(GunFrame frame)
    {
        if (_frameModel)
        {
            Destroy(_frameModel);
        }
        
        _sockets.Clear();

        if (!frame || !frame.PartPrefab)
        {
            return;
        }

        _frameModel = Instantiate(frame.PartPrefab, transform);
        _frameModel.transform.localPosition = Vector3.zero;
        _frameModel.transform.localRotation = Quaternion.identity;

        GunSocket[] sockets = _frameModel.GetComponentsInChildren<GunSocket>(true);

        foreach (GunSocket socket in sockets)
        {
            _sockets.TryAdd(socket.SocketType, socket);
        }
    }

    public bool GetSocket(GunPartType type, out GunSocket socket)
    {
        return _sockets.TryGetValue(type, out socket);
    }
}
