using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// An info box that stays on screen and shows the selected item's name, description, and icon, or nothing when no item
/// is selected. ItemGridUI updates it when its selection changes.
/// </summary>
public class ItemTooltip : MonoBehaviour
{
    [SerializeField] private TMP_Text _name;
    [SerializeField] private TMP_Text _description;
    [SerializeField] private Image _icon;

    private object _owner; // The grid whose item is showing, so another grid can't clear it.

  
    public void Show(IInventoryItem item, object owner)
    {
        if (item == null && owner != _owner) return;

        _owner = item != null ? owner : null;
        Display(item);
    }

    /// <summary>Empties the box.</summary>
    public void Clear()
    {
        _owner = null;
        Display(null);
    }

    /// <summary>Fills in the item, or blanks every field for null.</summary>
    private void Display(IInventoryItem item)
    {
        _name.text = item != null ? item.DisplayName : "";
        _description.text = item != null ? item.Description : "";

        if (_icon != null)
        {
            _icon.sprite = item != null ? item.Icon : null;
            _icon.enabled = item != null && item.Icon != null; // Hides the icon instead of showing a white box.
        }
    }
}