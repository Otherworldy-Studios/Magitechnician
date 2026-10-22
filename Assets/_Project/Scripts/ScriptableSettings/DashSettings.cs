using UnityEngine;

namespace ScriptableSettings
{
    [CreateAssetMenu(fileName = "DashSettings", menuName = "Settings/DashSettings")]
    public class DashSettings : ScriptableObject
    {
        [field: SerializeField] public float DashDistance {get; private set;} = 6f ;
        [field: SerializeField] public float DashDuration {get; private set;}  = 1f;
        [field: SerializeField] public float DashCooldown {get; private set;}  = 0.6f;
        
        [field: Header ("Invincibility Settings")]
        [field: SerializeField] public float InvincibilityStart {get; private set;}  = 0.1f;
        [field: SerializeField] public float InvincibilityDuration {get; private set;}  = 0.1f;
    }
}
