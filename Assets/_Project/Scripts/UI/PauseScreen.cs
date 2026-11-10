using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.UI;

public class PauseScreen : UIScreen
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button settingsButton;
   
    [SerializeField] private Button hubButton;
    [SerializeField] private Image pauseImage;
    
    
    private void Awake()
    {
        resumeButton.onClick.AddListener(OnResume);
        menuButton.onClick.AddListener(OnMenu);
        hubButton.onClick.AddListener(OnHub);
        settingsButton.onClick.AddListener(OnSettings);
    }


    private void OnResume()
    {
        UIManager.Instance.ResumeButtonPressed(); //OR pressing escape again but this should be implicit from the GM
    }

    private void OnMenu()
    {
        UIManager.Instance.MenuButtonPressed();
    }

    private void OnHub()
    {
        Debug.Log("On Hub");
        UIManager.Instance.ResetButtonPressed();
    }

    private void OnSettings()
    {
        UIManager.Instance.SettingsButtonPressed();
    }
}
