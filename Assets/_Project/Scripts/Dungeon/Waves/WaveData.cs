using System.Collections.Generic;
using UnityEngine;

public class WaveData : ScriptableObject
{
    [SerializeField]
    private List<Wave> _waves;
    
    public List<Wave> waves { get { return _waves; } }
}
