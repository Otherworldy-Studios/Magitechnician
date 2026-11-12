using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>The settings screen. UIManager.SettingsButtonPressed opens it. Placeholder: nothing is built yet.</summary>
// The empty Start and Update from Unity's script template were removed, so Unity doesn't call them every frame.
public class SettingsScreen : UIScreen
{
    [SerializeField, FormerlySerializedAs("backButton")] private Button _backButton;
    [SerializeField, FormerlySerializedAs("mouseSensitivitySlider")] private Slider _mouseSensitivitySlider;

    private void Awake()
    {
        _backButton.onClick.AddListener(OnBack);
        _mouseSensitivitySlider.onValueChanged.AddListener(delegate {OnMouseSensitivity();});
    }

    private void OnBack()
    {
        UIManager.Instance.BackButtonPressed();
    }

    private void OnMouseSensitivity()
    {
        UIManager.Instance.MouseSensitivityChanged(_mouseSensitivitySlider.value);
    }
}
