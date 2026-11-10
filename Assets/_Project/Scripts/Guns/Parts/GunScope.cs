using UnityEngine;
using UnityEngine.Serialization;

/// <summary>A scope part. Usually changes spread and range through its modifiers.</summary>
[CreateAssetMenu(menuName = "Guns/Parts/Gun Scope")]
public class GunScope : GunPart
{
    [Tooltip("Unused: the game has no zoom. Kept for now.")]
    [SerializeField, FormerlySerializedAs("Zoom")] private float _zoom;

    public float Zoom => _zoom;

    public override GunPartType PartType => GunPartType.Scope;
}
