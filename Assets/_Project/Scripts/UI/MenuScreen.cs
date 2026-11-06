using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuScreen : UIScreen
{

    [SerializeField] private Image menuImage;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button startButton;

    private void Awake()
    {
        exitButton.onClick.AddListener(OnExit);
        startButton.onClick.AddListener(OnStart);
    }

    private void OnExit()
    {
        UIManager.instance.QuitButtonPressed();
    }

    private void OnStart()
    {
        
        UIManager.instance.StartButtonPressed();
    }
}
