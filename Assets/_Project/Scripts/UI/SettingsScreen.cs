using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The settings screen. UIManager.SettingsButtonPressed opens it. Placeholder: nothing is built yet.</summary>
// The empty Start and Update from Unity's script template were removed, so Unity doesn't call them every frame.
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
