using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// The screen for picking one of the gun parts a cleared room rolled. Only has a Resume button so far; ShowChoices and
/// Pick are empty. Hooking it up to Room.OnRoomCleared and the loot pool is task 7.
/// </summary>
public class RewardScreen : UIScreen
{
    [SerializeField, FormerlySerializedAs("resumeButton")] private Button _resumeButton;

    protected RoomCompletionLootPool _lootPool; // The pool the shown parts came from. Not set yet.
    protected Gun _playerGun;                   // The gun the picked part goes on. Not set yet.

    /// <summary>Connects the Resume button.</summary>
    private void Awake()
    {
        _resumeButton.onClick.AddListener(OnResume);
    }

    /// <summary>Resume button. Closes the screen and unpauses.</summary>
    private void OnResume()
    {
        UIManager.Instance.ResumeButtonPressed();
    }

    /// <summary>Will show the rolled parts. Empty until task 7.</summary>
    public void ShowChoices()
    {
    }

    /// <summary>Will equip the chosen part and return the others to the pool. Empty until task 7.</summary>
    private void Pick(int index)
    {
    }
}
