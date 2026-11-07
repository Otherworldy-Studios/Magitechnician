using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuScreen : UIScreen
{

    [SerializeField] private Image menuImage;
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;

    private void Awake()
    {
        exitButton.onClick.AddListener(OnExit);
        startButton.onClick.AddListener(OnStart);
    }

    private void OnExit()
    {
        UIManager.Instance.QuitButtonPressed();
    }

    private void OnStart()
    {
        
        UIManager.Instance.StartButtonPressed();
    }
}
