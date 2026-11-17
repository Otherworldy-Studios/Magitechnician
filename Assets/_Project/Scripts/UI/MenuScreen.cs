using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>The main menu: Start, Settings, and Quit. Each button calls UIManager, which asks GameManager.</summary>
public class MenuScreen : UIScreen
{
    [SerializeField, FormerlySerializedAs("menuImage")] private Image _menuImage; // The background. Not read by this script.
    [SerializeField, FormerlySerializedAs("startButton")] private Button _startButton;
    [SerializeField, FormerlySerializedAs("exitButton")] private Button _exitButton;
    [SerializeField, FormerlySerializedAs("settingsButton")] private Button _settingsButton;

    /// <summary>Connects the buttons.</summary>
    private void Awake()
    {
        _exitButton.onClick.AddListener(OnExit);
        _startButton.onClick.AddListener(OnStart);
        _settingsButton.onClick.AddListener(OnSettings);
    }

    /// <summary>Quit button.</summary>
    private void OnExit()
    {
        UIManager.Instance.QuitButtonPressed();
    }

    /// <summary>Start button. GameManager loads the Hub.</summary>
    private void OnStart()
    {
        UIManager.Instance.StartButtonPressed();
    }

    /// <summary>Settings button.</summary>
    private void OnSettings()
    {
        UIManager.Instance.SettingsButtonPressed();
    }
}
