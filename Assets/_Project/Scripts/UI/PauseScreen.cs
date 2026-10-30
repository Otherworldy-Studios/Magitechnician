using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.UI;

public class PauseScreen : UIScreen
{
    [SerializeField] private Button unpauseButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button hubButton;
    
    
    private void Awake()
    {
        //unpauseButton.onClick.AddListener();
        //menuButton.onClick.AddListener();
        //hubButton.onClick.AddListener();
        //settingsButton.onClick.AddListener();

    }
    
    
}
