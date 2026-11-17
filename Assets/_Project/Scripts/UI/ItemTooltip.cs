using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shows an item's name, description, and icon. ItemGridUI shows it when a slot is hovered.</summary>
public class ItemTooltip : MonoBehaviour
{
    // INTEGRATION: these were private and not serialized, so they were never assigned. Now set in the Inspector.
    [SerializeField] private TMP_Text _name;
    [SerializeField] private TMP_Text _description;
    [SerializeField] private Image _icon;

    /// <summary>Fills in the item and turns the tooltip on. Does nothing for a null item.</summary>
    // INTEGRATION: was private, so nothing could call it. Now public for ItemGridUI.
    public void Show(IInventoryItem item)
    {
        if (item == null) return;

        _name.text = item.DisplayName;
        _description.text = item.Description;
        _icon.sprite = item.Icon;
        _icon.enabled = item.Icon != null; // INTEGRATION: new. Hides the icon instead of showing a white box.

        gameObject.SetActive(true);
    }

    /// <summary>Turns the tooltip off.</summary>
    // INTEGRATION: was private. Now public for ItemGridUI.
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
