using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>Shown when the run ends in a win. Shows the run time and has a button back to the Hub.</summary>
public class WinScreen : UIScreen
{
    [SerializeField, FormerlySerializedAs("hubButton")] private Button _hubButton;
    [SerializeField, FormerlySerializedAs("runTimeText")] private TMP_Text _runTimeText;
    [SerializeField] private TMP_Text _statsText;
    [SerializeField, FormerlySerializedAs("winImage")] private Image _winImage; // The background. Not read by this script.

    /// <summary>Connects the Hub button.</summary>
    private void Awake()
    {
        _hubButton.onClick.AddListener(OnHub);
    }

    /// <summary>Shows the run time when the screen opens.</summary>
    protected override void OnShown()
    {
        _runTimeText.text = $"Run Time: {CollectEndTime():F1} seconds";

        GameManager gm = GameManager.Instance;

        string roomTimes = string.Join(", ", gm.RoomTimes);

        _statsText.text =
            $"Enemies Killed: {gm.EnemiesKilled}\n" +
            $"Damage Taken: {gm.DamageTaken:F0}\n" +
            $"Rooms Cleared: {gm.RoomsCleared}\n" +
            $"Room Times: {roomTimes}";
    }

    /// <summary>Hub button.</summary>
    private void OnHub()
    {
        Debug.Log("On Hub");
        UIManager.Instance.ResetButtonPressed();
    }

    /// <summary>The run's length in seconds, from GameManager.</summary>
    private float CollectEndTime()
    {
        return GameManager.Instance.EndTime;
    }
}
