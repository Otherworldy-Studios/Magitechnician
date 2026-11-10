using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltip : MonoBehaviour
{
    [SerializeField] private TMP_Text _name;
    [SerializeField] private TMP_Text _description;
    [SerializeField] private Image _icon;

    public void Show(IInventoryItem item)
    {
        if (item == null) return;

        _name.text = item.DisplayName;
        _description.text = item.Description;
        _icon.sprite = item.Icon;
        _icon.enabled = item.Icon != null;

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}