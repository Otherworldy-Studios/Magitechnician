using Sirenix.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shown when the run ends in a loss. Shows the run time and several other
/// stats during gameplay which is used in a calculation which determines player ranking.
/// Also has a button back to the Hub.</summary>
public class DeathScreen : UIScreen
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

    [Header("Image Objects")]
    [SerializeField] private Image _finalRankImage;
    [Tooltip("Rank sprites in ascending order, lowest rank first.")]
    [SerializeField] private Sprite[] _rankSprites = new Sprite[5];
    [Tooltip("The background. Not read by this script.")]
    [SerializeField] private Image _deathImage;

    // Points per unit of each stat. Damage taken lowers the score.
    private float _crystalsCollectedMultiplier = 1f;
    private float _damageTakenMultiplier = 10f;
    private float _damageDealtMultiplier = 10f;
    private float _enemiesKilledMultiplier = 50f;

    private void Awake()
    {
        _hubButton.onClick.AddListener(OnHub);
    }

    protected override void OnShown()
    {
        RunStats stats = GameManager.Instance.RunStats;
        int score = TotalScoreCalculation(stats);

        _runTimeText.text = $"Run Time: {(int)GameManager.Instance.EndTime}";
        _crystalsCollectedText.text = $"Crystals Collected: {stats.CrystalsCollected}";
        _damageTakenText.text = $"Damage Taken: {stats.DamageTaken}";
        _damageDealtText.text = $"Damage Dealt: {stats.DamageDealt}";
        _enemiesKilledText.text = $"Enemies Killed: {stats.EnemiesKilled}";
        _totalScoreText.text = $"Total Score: {score}";

        _finalRankImage.sprite = GetRankSprite(score);
    }

    private void OnHub()
    {
        UIManager.Instance.ResetButtonPressed();
    }

    /// <summary>
    /// Adds crystals, damage dealt, and kills, and subtracts damage taken, each times its multiplier.
    /// Run time doesn't count on a loss. Never below 0.
    /// </summary>
    private int TotalScoreCalculation(RunStats stats)
    {
        float score =
            stats.CrystalsCollected * _crystalsCollectedMultiplier +
            stats.DamageDealt * _damageDealtMultiplier +
            stats.EnemiesKilled * _enemiesKilledMultiplier -
            stats.DamageTaken * _damageTakenMultiplier;

        return Mathf.Max(0, (int)score);
    }

    /// <summary>The rank Sprite (chosen from _rankSprites[]) according to the range the score falls under.</summary>
    private Sprite GetRankSprite(int score)
    {
        if (_rankSprites.IsNullOrEmpty())
        {
            Debug.LogWarning("The Rank Sprites array is empty. Check the DeathScreen in UIManager.", this);
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