using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.UI;

public class PauseScreen : UIScreen
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button hubButton;
    
    
    private void Awake()
    {
        resumeButton.onClick.AddListener(OnResume);
        menuButton.onClick.AddListener(OnMenu);
        hubButton.onClick.AddListener(OnHub);
        settingsButton.onClick.AddListener(OnSettings);
    }


    private void OnResume()
    {
        UIManager.instance.PauseButtonPressed(); //OR pressing escape again but this should be implicit from the GM
    }

    private void OnMenu()
    {
        UIManager.instance.MenuButtonPressed();
    }

    private void OnHub()
    {
        UIManager.instance.ResetButtonPressed();
    }

    private void OnSettings()
    {
        UIManager.instance.PauseButtonPressed();
    }
}
