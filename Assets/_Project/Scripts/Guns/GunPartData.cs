using UnityEngine;

[CreateAssetMenu(menuName = "Guns/Gun Part")]
public class GunPartData : ScriptableObject
{
    public GameObject partPrefab;
    public string partName;
    public GunPartType partType;
}
