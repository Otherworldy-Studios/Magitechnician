using UnityEngine;

public abstract class GunPart : ScriptableObject
{
    [SerializeField] private GameObject _partPrefab;
    [SerializeField] private string _partName;

    public GameObject PartPrefab => _partPrefab;
    public string PartName => _partName;
    public abstract GunPartType PartType { get; }
}
