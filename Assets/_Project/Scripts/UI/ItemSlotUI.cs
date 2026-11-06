using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    private Image _icon;
    private TMP_Text _name;

    [SerializeField] ItemCategory _assignedCategory;

    public void SetItem (IInventoryItem item) 
    {
        if (item.Category != _assignedCategory) return;
        _icon.sprite = item.Icon;
        _name.text = item.DisplayName;
    }

    public void Clear()
    {
        _icon.sprite = null;
        _name.text = "";
    }
}
