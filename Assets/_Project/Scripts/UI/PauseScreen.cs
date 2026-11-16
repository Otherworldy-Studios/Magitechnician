using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>The pause menu: Resume, Main Menu, Hub, and Settings. Each button calls UIManager.</summary>
public class PauseScreen : UIScreen
{
    [SerializeField, FormerlySerializedAs("resumeButton")] private Button _resumeButton;
    [SerializeField, FormerlySerializedAs("menuButton")] private Button _menuButton;
    [SerializeField, FormerlySerializedAs("settingsButton")] private Button _settingsButton;
    [SerializeField, FormerlySerializedAs("hubButton")] private Button _hubButton;
    [SerializeField, FormerlySerializedAs("pauseImage")] private Image _pauseImage; // The background. Not read by this script.

    /// <summary>Connects the buttons.</summary>
    private void Awake()
    {
        _resumeButton.onClick.AddListener(OnResume);
        _menuButton.onClick.AddListener(OnMenu);
        _hubButton.onClick.AddListener(OnHub);
        _settingsButton.onClick.AddListener(OnSettings);
    }

    /// <summary>Resume button. Pressing Pause again does the same through GameManager.</summary>
    private void OnResume()
    {
        UIManager.Instance.ResumeButtonPressed();
    }

    /// <summary>Main Menu button.</summary>
    private void OnMenu()
    {
        UIManager.Instance.MenuButtonPressed();
    }

    /// <summary>Hub button. Ends the run and returns to the Hub.</summary>
    private void OnHub()
    {
        Debug.Log("On Hub");
        UIManager.Instance.ResetButtonPressed();
    }

    /// <summary>Settings button.</summary>
    private void OnSettings()
    {
        UIManager.Instance.SettingsButtonPressed();
    }
}
