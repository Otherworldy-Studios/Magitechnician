using UnityEngine;

namespace ScriptableSettings
{
    [CreateAssetMenu(fileName = "PlayerMovementSettings", menuName = "Settings/Player Movement")]
    public class PlayerMovementSettings : ScriptableObject
    {
        [field: Header("Settings")]
        [field: SerializeField] public float AimSpeed { get; private set; } = 2.5f;
        [field: SerializeField] public float WalkSpeed { get; private set; } = 4f;
        [field: SerializeField] public float SprintSpeed { get; private set; } = 7f;
        [field: SerializeField] public float JumpHeight { get; private set; } = 1.5f;
        [field: SerializeField] public float Gravity { get; private set; } = -20f;
        [field: SerializeField] public float GroundGravity { get; private set; } = -5f;
        
        [field: Header("Acceleration")]
        [field: SerializeField] public float SprintAcceleration { get; private set; } = 18f;
        [field: SerializeField] public float WalkAcceleration { get; private set; } = 25f;
        [field: SerializeField] public float AirborneAcceleration { get; private set; } = 8f;
        [field: SerializeField] public float AimingAcceleration { get; private set; } = 20f;
        [field: SerializeField] public float IdleAcceleration { get; private set; } = 35f;
    }
}
