using System;
using UnityEngine;

/// <summary>
/// Placeholder for the boss. Raises OnBossDefeated, which GameManager turns into a victory.
/// Replace with the real boss and delete this file.
/// </summary>
// INTEGRATION: moved out of GameManager.cs, because each MonoBehaviour needs its own file named after the class.
public class PH_Boss : MonoBehaviour
{
    /// <summary>Fires when the boss dies. GameManager.BossDefeated listens to it.</summary>
    public static event Action OnBossDefeated;

    /// <summary>Raises OnBossDefeated. Nothing calls this yet.</summary>
    private void OnBossDead()
    {
        OnBossDefeated?.Invoke();
    }
}
