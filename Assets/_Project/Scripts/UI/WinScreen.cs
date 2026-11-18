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
    [SerializeField, FormerlySerializedAs("damageDealtText")] private TMP_Text _damageDealtText;
    [SerializeField,  FormerlySerializedAs("enemiesKilledText")] private TMP_Text _enemiesKilledText;
    [SerializeField,  FormerlySerializedAs("totalScoreText")] private TMP_Text _totalScoreText;
    
    //Multipliers for each key stat
    private float _runTimeMultiplier = 1f;
    private float _crystalsCollectedMultiplier = 1f;
    private float _damageTakenMultiplier = 10f;
    private float _damageDealtMultiplier = 10f;
    private float _enemiesKilledMultiplier = 50f;
    
    // Image used to display the final ranking, going from based on run stats calculation
    [SerializeField, FormerlySerializedAs("finalRankImage")] private Image _finalRankImage;
    [SerializeField, FormerlySerializedAs("listOfRanks")] private Sprite[] _rankSprites;
    
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
        _damageDealtText.text = $"Damage Dealt: {TotalDamageDealt()}";
        _enemiesKilledText.text = $"Enemies Killed: {TotalEnemiesKilled()}";
        _totalScoreText.text = $"Total Score: {CalculateTotalScore()}";

        _finalRankImage.sprite = SetRankingImage();
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
        // Blocked, need to wait on Ali to implement
        return 1234; // For testing
    }
    
    private int TotalDamageDealt()
    {
        // Blocked, need to wait on Ali to implement
        return 4321;
    }

    private int TotalEnemiesKilled()
    {
        // Blocked, need to wait on Ali to implement
        return 12;
    }

    private int CalculateTotalScore()
    {
        return
            (int)(
                CollectEndTime() * _runTimeMultiplier +
                TotalCrystalsCollected() * _crystalsCollectedMultiplier +
                TotalDamageTaken() * _damageTakenMultiplier +
                TotalDamageDealt() * _damageDealtMultiplier +
                TotalEnemiesKilled() * _enemiesKilledMultiplier
            )
        ;
    }
    
    private Sprite SetRankingImage()
    {
        
    }
}
