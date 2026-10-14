using UnityEngine;

[CreateAssetMenu(menuName = "Guns/Gun Part")]
public class GunPartData : ScriptableObject
{
    [SerializeField] private GameObject _partPrefab;
    [SerializeField] private string _partName;
    [SerializeField] private GunPartType _partType;
    //[SerializeField] private GunStats _statModifiers;

    public GameObject PartPrefab => _partPrefab;
    public string PartName => _partName;
    public GunPartType PartType => _partType;
}
