using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Marks a point on the gun body where a part attaches, and which part type fits there.
/// Gun.BuildGun finds these in its children and places each part's model on the matching socket.
/// </summary>
public class GunSocket : MonoBehaviour
{
    [Tooltip("The part type that attaches here.")]
    [SerializeField, FormerlySerializedAs("SocketType")] private GunPartType _socketType;

    /// <summary>The part type that attaches here.</summary>
    public GunPartType SocketType => _socketType;
}
