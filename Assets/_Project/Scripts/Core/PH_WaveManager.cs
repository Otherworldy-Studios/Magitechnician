using System;
using UnityEngine;

/// <summary>
/// Placeholder for the wave manager, which Team 3 builds in Sprint 3. Until then, RoomEncounter clears rooms
/// from enemies placed by hand. Replace with WaveManager and delete this file.
/// </summary>
// INTEGRATION: moved out of GameManager.cs, because each MonoBehaviour needs its own file named after the class.
public class PH_WaveManager : MonoBehaviour
{
    /// <summary>Fires when a room's last wave is defeated. GameManager.RoomCleared listens to it.</summary>
    public static event Action OnRoomCleared;

    /// <summary>Raises OnRoomCleared. Nothing calls this yet.</summary>
    private void OnRoomCompleted()
    {
        OnRoomCleared?.Invoke();
    }
}
