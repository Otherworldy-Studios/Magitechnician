using System;
using System.Runtime.CompilerServices;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_GlobalSettings", menuName = "Settings/GlobalSettings")]
public class GlobalSettings : ScriptableObject
{
    private static GlobalSettings _instance;
    public static GlobalSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                // Automatically loads the asset if it is placed in a "Resources" folder
                _instance = Resources.Load<GlobalSettings>("SO_GlobalSettings");
            }
            return _instance;
        }
    }
    public RarityColors RarityColorSettings;


}

[Serializable]
public struct RarityColors
{
    public Color Common;
    public Color Uncommon;
    public Color Rare;
    public Color Epic;
}