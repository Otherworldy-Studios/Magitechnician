using UnityEngine;

[CreateAssetMenu(fileName = "StatBlock", menuName = "Stats/Stat Block")]

[System.Serializable]
public class StatBlock : ScriptableObject
{
    [SerializeField] private Stat[] stats;

    public Stat[] GetStats()
    {
        return stats;
    }
}
