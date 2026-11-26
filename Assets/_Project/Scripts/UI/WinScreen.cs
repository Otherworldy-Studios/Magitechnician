using Sirenix.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>Shown when the run ends in a win. Shows the run time and several other
/// stats during gameplay which is used in a calculation which determines player ranking.
/// Also has a button back to the Hub.</summary>
public class WinScreen : UIScreen
{
    [Header("Buttons")]
    [SerializeField] private Button _hubButton;

    [Header("Text Objects")]
    [SerializeField] private TMP_Text _runTimeText;
    [SerializeField] private TMP_Text _crystalsCollectedText;
    [SerializeField] private TMP_Text _damageTakenText;
    [SerializeField] private TMP_Text _damageDealtText;
    [SerializeField] private TMP_Text _enemiesKilledText;
    [SerializeField] private TMP_Text _totalScoreText;
    [SerializeField] private TMP_Text _statsText;

    [Header("Image Objects")]
    [SerializeField] private Image _finalRankImage;
    [Tooltip("Rank sprites in ascending order, lowest rank first.")]
    [SerializeField] private Sprite[] _rankSprites = new Sprite[5];
    [Tooltip("The background. Not read by this script.")]
    [SerializeField] private Image _winImage;

    // Points per unit of each stat. Damage taken lowers the score.
    private float _crystalsCollectedMultiplier = 1f;
    private float _damageTakenMultiplier = 10f;
    private float _damageDealtMultiplier = 10f;
    private float _enemiesKilledMultiplier = 50f;

    // A win faster than the par time earns the time bonus for every second under it.
    private float _parTime = 900f;
    private float _timeBonusPerSecond = 10f;

    private void Awake()
    {
        _hubButton.onClick.AddListener(OnHub);
    }

    protected override void OnShown()
    {
        RunStats stats = GameManager.Instance.RunStats;
        float runTime = GameManager.Instance.EndTime;
        int score = TotalScoreCalculation(stats, runTime);

        _runTimeText.text = $"Run Time: {(int)runTime}";
        _crystalsCollectedText.text = $"Crystals Collected: {stats.CrystalsCollected}";
        _damageTakenText.text = $"Damage Taken: {stats.DamageTaken}";
        _damageDealtText.text = $"Damage Dealt: {stats.DamageDealt}";
        _enemiesKilledText.text = $"Enemies Killed: {stats.EnemiesKilled}";
        _totalScoreText.text = $"Total Score: {score}";
        if (stats.RoomTimes.Count == 0)
        {
            _statsText.text = "Room Times: N/A";
        }
        else
        {
            string roomTimes = string.Join(", ",
                stats.RoomTimes.Select(time => $"{time:F1}s"));

            _statsText.text = $"Room Times: {roomTimes}";
        }

        _finalRankImage.sprite = GetRankSprite(score);
    }

    private void OnHub()
    {
        UIManager.Instance.ResetButtonPressed();
    }

    /// <summary>
    /// Adds crystals, damage dealt, kills, and the time bonus, and subtracts damage taken, each times its multiplier.
    /// Never below 0.
    /// </summary>
    private int TotalScoreCalculation(RunStats stats, float runTime)
    {
        float timeBonus = Mathf.Max(0f, _parTime - runTime) * _timeBonusPerSecond;

        float score =
            stats.CrystalsCollected * _crystalsCollectedMultiplier +
            stats.DamageDealt * _damageDealtMultiplier +
            stats.EnemiesKilled * _enemiesKilledMultiplier +
            timeBonus -
            stats.DamageTaken * _damageTakenMultiplier;

        return Mathf.Max(0, (int)score);
    }

    /// <summary>The rank Sprite (chosen from _rankSprites[]) according to the range the score falls under.</summary>
    private Sprite GetRankSprite(int score)
    {
        if (_rankSprites.IsNullOrEmpty())
        {
            Debug.LogWarning("The Rank Sprites array is empty. Check the WinScreen in UIManager.", this);
            return null;
        }

        int threshold = 1000;

        for (int rank = 0; rank < _rankSprites.Length - 1; rank++)
        {
            if (score < threshold)
                return _rankSprites[rank];

            threshold *= 10;
        }

        return _rankSprites[_rankSprites.Length - 1];
    }
}