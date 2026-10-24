using UnityEngine;

// Used to group multiple base stats into one SO. Enables the creation of template stats
[CreateAssetMenu(fileName = "StatBlock", menuName = "Stats/Stat Block")]
public class StatBlock : ScriptableObject
{
    [SerializeField] private Stat[] stats;

    public Stat[] GetStats()
    {
        return stats;
    }
}
