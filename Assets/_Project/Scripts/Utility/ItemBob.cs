using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Moves an object up and down on a sine wave around its starting local position.
/// Goes on a pickup's model, so it stands out on the ground.
/// </summary>
// INTEGRATION: new script, for the pickups that chests and rooms will drop.
public class ItemBob : MonoBehaviour
{
    [Tooltip("How fast the object moves up and down.")]
    [SerializeField, FormerlySerializedAs("bobSpeed")] private float _bobSpeed = 5f;   
    [Tooltip("How far the object moves above and below its start, in meters.")]
    [SerializeField, FormerlySerializedAs("bobAmount")] private float _bobAmount = 0.05f;

    private Vector3 _startPosition; // The local position when the object started. The movement is around this point.

    /// <summary>Records the starting local position.</summary>
    private void Start()
    {
        _startPosition = transform.localPosition;
    }

    /// <summary>Sets the height from a sine of the current time.</summary>
    private void Update()
    {
        float newY = _startPosition.y + Mathf.Sin(Time.time * _bobSpeed) * _bobAmount;
        transform.localPosition = new Vector3(_startPosition.x, newY, _startPosition.z);
    }
}
