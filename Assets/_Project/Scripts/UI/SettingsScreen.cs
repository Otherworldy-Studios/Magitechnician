using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingsScreen : UIScreen
{
    [SerializeField] private Button backButton;

    private void Awake()
    {
        backButton.onClick.AddListener(OnBack);
    }

    private void OnBack()
    {
        UIManager.instance.BackButtonPressed();
    }
}
