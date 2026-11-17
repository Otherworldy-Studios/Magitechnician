using UnityEngine;

namespace ScriptableSettings
{
    /// <summary>
    /// Player movement values that upgrades don't change: jump, gravity, and acceleration. The move speed comes from
    /// Stat_Speed; LocomotionSpeed is only its fallback, and the player moves at the same speed while aiming.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerMovementSettings", menuName = "Settings/Player Movement")]
    public class PlayerMovementSettings : ScriptableObject
    {
        /// <summary>Unused: the player moves at the same speed while aiming.</summary>
        [field: Header("Settings")]
        [field: SerializeField] public float AimSpeed { get; private set; } = 2.5f;
        /// <summary>Meters per second. Fallback for Stat_Speed.</summary>
        [field: SerializeField] public float LocomotionSpeed { get; private set; } = 6f;

        /// <summary>Jump height in meters when the jump button is tapped.</summary>
        [field: Header("Jump")]
        [field: SerializeField] public float MinJumpHeight { get; private set; } = 1f;
        /// <summary>Jump height in meters when the jump button is held.</summary>
        [field: SerializeField] public float MaxJumpHeight { get; private set; } = 1.5f;
        /// <summary>Seconds after leaving a ledge during which a jump still works.</summary>
        [field: SerializeField] public float CoyoteTime { get; private set; } = 0.1f;
        /// <summary>Seconds before landing during which a jump press is remembered.</summary>
        [field: SerializeField] public float JumpBufferTime { get; private set; } = 0.1f;

        /// <summary>Downward acceleration in the air.</summary>
        [field: Header("Gravity")]
        [field: SerializeField] public float Gravity { get; private set; } = -20f;
        /// <summary>Small downward speed while grounded, to keep the CharacterController on slopes.</summary>
        [field: SerializeField] public float GroundGravity { get; private set; } = -5f;

        [field: Header("Acceleration")]
        // How fast the player reaches the target speed in each movement state.
        [field: SerializeField] public float LocomotionAcceleration { get; private set; } = 18f;
        [field: SerializeField] public float AirborneAcceleration { get; private set; } = 8f;
        [field: SerializeField] public float AimingAcceleration { get; private set; } = 20f;
        [field: SerializeField] public float IdleAcceleration { get; private set; } = 35f;
    }
}
