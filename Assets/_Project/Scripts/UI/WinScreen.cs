using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>Shown when the run ends in a win. Shows the run time and has a button back to the Hub.</summary>
public class WinScreen : UIScreen
{
    // Button to return back to the Hub area and do another rerun
    [SerializeField, FormerlySerializedAs("hubButton")] private Button _hubButton;
    
    // Text fields for showing all the key stats in a run
    [SerializeField, FormerlySerializedAs("runTimeText")] private TMP_Text _runTimeText;
    [SerializeField, FormerlySerializedAs("crystalsCollectedText")] private TMP_Text _crystalsCollectedText;
    [SerializeField, FormerlySerializedAs("damageTakenText")] private TMP_Text _damageTakenText;
    [SerializeField, FormerlySerializedAs("charmsActivatedText")] private TMP_Text _charmsActivatedText;
    [SerializeField,  FormerlySerializedAs("consumablesUsedText")] private TMP_Text _consumablesUsedText;
    [SerializeField,  FormerlySerializedAs("totalScoreText")] private TMP_Text _totalScoreText;
    
    // Image used to display the final ranking, going from based on run stats calculation
    [SerializeField, FormerlySerializedAs("finalRankImage")] private Image _finalRankImage;
    [SerializeField, FormerlySerializedAs("winImage")] private Image _winImage; // The background. Not read by this script.

    /// <summary>Connects the Hub button.</summary>
    private void Awake()
    {
        _hubButton.onClick.AddListener(OnHub);
    }

    /// <summary>Shows the run time when the screen opens.</summary>
    protected override void OnShown()
    {
        _runTimeText.text = $"Run Time: {CollectEndTime()}";
        _crystalsCollectedText.text = $"Crystals Collected: {TotalCrystalsCollected()}";
        _damageTakenText.text = $"Damage Taken: {TotalDamageTaken()}";
        _charmsActivatedText.text = $"Charms Activated: {TotalCharmsActivated()}";
        _consumablesUsedText.text = $"Consumables Used: {TotalConsumablesUsed()}";
        _totalScoreText.text = $"Total Score: {CalculateTotalScore()}";
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

    private int TotalCrystalsCollected()
    {
        return GameManager.Instance.Progress.Crystals;
    }

    private int TotalDamageTaken()
    {
        // Need to know where total damage taken is stored. Doesn't seem GameManager is recording that,
        // though it is taking in the Player's Health component.
        return 0;
    }
    
    private int TotalCharmsActivated()
    {
        // Need to know where total charm use is stored. Doesn't seem GameManager is recording that.
        return 0;
    }

    private int TotalConsumablesUsed()
    {
        // Need to know where total consumable use is stored. Doesn't seem GameManager is recording that.
        return 0;
    }

    private int CalculateTotalScore()
    {
        return
            (int)CollectEndTime() * 1 +
            TotalCrystalsCollected() * 1 +
            TotalDamageTaken() * 10 +
            TotalCharmsActivated() * 100 +
            TotalConsumablesUsed() * 100
        ;
    }
}
