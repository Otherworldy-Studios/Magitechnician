using UnityEngine;

namespace ScriptableSettings
{
    /// <summary>
    /// Dash timing and invincibility settings. Dash reads distance, duration, cooldown, and invincibility length from
    /// the dash stats, and uses these values only as fallbacks when a stat isn't assigned. Invincibility start and
    /// whether invincibility is on always come from here.
    /// </summary>
    [CreateAssetMenu(fileName = "DashSettings", menuName = "Settings/DashSettings")]
    public class DashSettings : ScriptableObject
    {
        /// <summary>Meters covered by one dash. Fallback for Stat_DashDistance.</summary>
        [field: SerializeField] public float DashDistance { get; private set; } = 6f;
        /// <summary>Seconds one dash lasts. Fallback for Stat_DashDuration.</summary>
        [field: SerializeField] public float DashDuration { get; private set; } = 1f;
        /// <summary>Seconds after a dash ends before the next. Fallback for Stat_DashCooldown.</summary>
        [field: SerializeField] public float DashCooldown { get; private set; } = 0.6f;

        /// <summary>Seconds into the dash before invincibility starts.</summary>
        [field: Header("Invincibility Settings")]
        [field: SerializeField] public float InvincibilityStart { get; private set; } = 0.1f;
        /// <summary>Seconds of invincibility. Fallback for Stat_DashInvincibility.</summary>
        [field: SerializeField] public float InvincibilityDuration { get; private set; } = 0.1f;
        /// <summary>Whether the dash gives invincibility at all.</summary>
        [field: SerializeField] public bool InvincibilityEnabled { get; private set; } = true;
    }
}
