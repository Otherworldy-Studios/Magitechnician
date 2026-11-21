using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// One row of GunStatsPanel: a stat's name, its current value, and how the selected part would change it. The change
/// shows an arrow for the direction, and is green when the change is better and red when it's worse, going by
/// StatDefinition.HigherIsBetter. Hovering the row shows the stat's description in the StatTooltip.
/// </summary>
public class GunStatRowUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _valueText;
    
    [SerializeField] private TMP_Text _changeText;

    [Header("Look")]
  
    [SerializeField] private string _numberFormat = "0.##";
    
    [SerializeField] private string _upArrow = "▲";
    
    [SerializeField] private string _downArrow = "▼";
    [SerializeField] private Color _betterColor = new(0.3f, 0.85f, 0.35f);
    [SerializeField] private Color _worseColor = new(0.9f, 0.3f, 0.3f);

    private StatTooltip _tooltip;
    private float _currentValue;

    /// <summary>The stat this row shows.</summary>
    public StatDefinition Stat { get; private set; }

    /// <summary>Shows a stat and its current value, with no preview.</summary>
    public void SetStat(StatDefinition stat, float currentValue, StatTooltip tooltip)
    {
        Stat = stat;
        _currentValue = currentValue;
        _tooltip = tooltip;

        _nameText.text = string.IsNullOrEmpty(stat.DisplayName) ? stat.name : stat.DisplayName;
        _valueText.text = currentValue.ToString(_numberFormat);
        ClearPreview();
    }

    /// <summary>Shows the change from the current value to the previewed one. Shows nothing when they're equal.</summary>
    public void ShowPreview(float previewValue)
    {
        float change = previewValue - _currentValue;

        
        if (change.ToString(_numberFormat) == "0" || change.ToString(_numberFormat) == "-0")
        {
            ClearPreview();
            return;
        }

        bool wentUp = change > 0f;
        bool isBetter = wentUp == Stat.HigherIsBetter;
        string arrow = wentUp ? _upArrow : _downArrow;
        string sign = wentUp ? "+" : "";

        _changeText.text = $"{arrow} {sign}{change.ToString(_numberFormat)}";
        _changeText.color = isBetter ? _betterColor : _worseColor;
    }

    public void ClearPreview()
    {
        _changeText.text = "";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_tooltip == null || Stat == null || string.IsNullOrEmpty(Stat.Description)) return;

        _tooltip.Show(_nameText.text, Stat.Description, eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (_tooltip != null) _tooltip.MoveTo(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_tooltip != null) _tooltip.Hide();
    }

    private void OnDisable()
    { 
        if (_tooltip != null) _tooltip.Hide();
    }
}