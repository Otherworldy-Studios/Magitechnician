using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class BossHealthBar : HealthBar
{
    [Header("Boss")]
  
    [SerializeField] private GameObject _content;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private string _bossName = "Stone Guardian";

    [Header("Chip")]
    [Tooltip("Sits behind Fill, and drains down to it after each hit. Optional.")]
    [SerializeField] private Image _chipFill;
    [Tooltip("Seconds after a hit before the chip starts draining.")]
    [SerializeField, Min(0f)] private float _chipDelay = 0.5f;
    [Tooltip("How fast the chip drains, in bar lengths per second.")]
    [SerializeField, Min(0.01f)] private float _chipSpeed = 0.6f;

    [Header("Phase Marks")]
    [Tooltip("A thin image placed on the bar at each phase threshold. Optional.")]
    [SerializeField] private RectTransform _phaseMarkPrefab;
    [Tooltip("The rect the marks are placed across, usually the bar's background.")]
    [SerializeField] private RectTransform _phaseMarkArea;

    private readonly List<RectTransform> _phaseMarks = new();
    private float _chipFillAmount = 1f;
    private float _chipTimer;

    protected override bool BindsToPlayer => false;

    protected override void Awake()
    {
        base.Awake();

        if (_content != null)
            _content.SetActive(false);
    }

    protected override void Start()
    {
        base.Start();

        if (WaveManager.HasInstance)
            WaveManager.Instance.OnBossSpawned += HandleBossSpawned;
    }

    protected override void OnDestroy()
    {
        if (WaveManager.HasInstance)
            WaveManager.Instance.OnBossSpawned -= HandleBossSpawned;

        if (BoundHealth != null)
            BoundHealth.OnDied -= HandleBossDied;

        base.OnDestroy();
    }

    protected override void Update()
    {
        base.Update();

        if (_chipFill == null || BoundHealth == null) return;

       
        if (_chipFillAmount <= VisualHealth)
        {
            _chipFillAmount = VisualHealth;
        }
        else if (_chipTimer > 0f)
        {
            _chipTimer -= Time.deltaTime;
        }
        else
        {
            _chipFillAmount = Mathf.MoveTowards(_chipFillAmount, VisualHealth, _chipSpeed * Time.deltaTime);
        }

        _chipFill.fillAmount = _chipFillAmount;
    }

    protected override void OnHealthChanged(float healthPercent)
    {
        _chipTimer = _chipDelay;
    }

    private void HandleBossSpawned(EnemyBase boss)
    {
        if (BoundHealth != null)
            BoundHealth.OnDied -= HandleBossDied;

        Bind(boss.Health);
        if (BoundHealth == null) return;

        BoundHealth.OnDied += HandleBossDied;

        _chipFillAmount = 1f;
        _chipTimer = 0f;

        if (_nameText != null)
            _nameText.text = _bossName;

        PlacePhaseMarks(boss as StoneGuardian);

        if (_content != null)
            _content.SetActive(true);
    }

    private void HandleBossDied(GameObject boss)
    {
        if (BoundHealth != null)
            BoundHealth.OnDied -= HandleBossDied;

        Unbind();

        if (_content != null)
            _content.SetActive(false);
    }

    private void PlacePhaseMarks(StoneGuardian boss)
    {
        foreach (RectTransform mark in _phaseMarks)
        {
            Destroy(mark.gameObject);
        }
        _phaseMarks.Clear();

        if (boss == null || _phaseMarkPrefab == null || _phaseMarkArea == null) return;

        foreach (float threshold in boss.PhaseThresholds)
        {
            RectTransform mark = Instantiate(_phaseMarkPrefab, _phaseMarkArea);

            // Anchoring both sides at the threshold puts the mark at that share of the bar's width.
            mark.anchorMin = new Vector2(threshold, 0f);
            mark.anchorMax = new Vector2(threshold, 1f);
            mark.anchoredPosition = Vector2.zero;

            _phaseMarks.Add(mark);
        }
    }
}