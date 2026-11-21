using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lists the gun's stats, one GunStatRowUI per stat, and previews how a gun part would change them. RewardScreen calls
/// Show when it opens and Preview when the player selects a part. Hovering a row shows the stat's description in the
/// StatTooltip.
/// </summary>
public class GunStatsPanel : MonoBehaviour
{
    [Tooltip("The stats to list, in order. Stats the gun's StatBlock doesn't have are skipped.")]
    [SerializeField] private List<StatDefinition> _statsToShow = new();

    [Tooltip("The rows are created under this, so give it a Vertical Layout Group.")]
    [SerializeField] private Transform _rowParent;
    [SerializeField] private GunStatRowUI _rowPrefab;
    [Tooltip("Shared by every row. Put it last under the screen's Canvas, so it draws on top.")]
    [SerializeField] private StatTooltip _tooltip;

    private readonly List<GunStatRowUI> _rows = new(); // Created as needed and reused. Extra rows are turned off.
    private Gun _gun;

    /// <summary>Lists the gun's current stats with no preview. Hides the panel's rows when the gun is null.</summary>
    public void Show(Gun gun)
    {
        _gun = gun;
        Stats stats = gun != null ? gun.Stats : null;
        int shown = 0;

        if (stats != null)
        {
            foreach (StatDefinition stat in _statsToShow)
            {
                if (!stats.HasStat(stat)) continue;

                GunStatRowUI row = GetRow(shown);
                row.gameObject.SetActive(true);
                row.SetStat(stat, stats.GetValue(stat), _tooltip);
                shown++;
            }
        }

        for (int i = shown; i < _rows.Count; i++)
        {
            _rows[i].gameObject.SetActive(false);
        }

        if (_tooltip != null) _tooltip.Hide();
    }

    /// <summary>
    /// Shows how each stat would change with the part equipped, replacing the part in the same slot. Null clears the
    /// preview.
    /// </summary>
    public void Preview(GunPart part)
    {
        Stats stats = _gun != null ? _gun.Stats : null;

        foreach (GunStatRowUI row in _rows)
        {
            if (!row.gameObject.activeSelf) continue;

            if (part == null || stats == null)
            {
                row.ClearPreview();
                continue;
            }

            // The current part's modifiers come off, and the new part's go on.
            GunPart replaced = _gun.GetEquippedPart(part.PartType);
            float previewValue = stats.PreviewValue(row.Stat, replaced, part.Modifiers);
            row.ShowPreview(previewValue);
        }
    }

    /// <summary>Clears the preview and hides the tooltip. RewardScreen calls this when it closes.</summary>
    public void Hide()
    {
        Preview(null);
        _gun = null;

        if (_tooltip != null) _tooltip.Hide();
    }

    /// <summary>Returns the row at the index, creating it under the row parent if it doesn't exist yet.</summary>
    private GunStatRowUI GetRow(int index)
    {
        while (_rows.Count <= index)
        {
            _rows.Add(Instantiate(_rowPrefab, _rowParent));
        }

        return _rows[index];
    }
}