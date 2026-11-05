using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltip : MonoBehaviour
{
    private TMP_Text _name;
    private TMP_Text _description;

    private Image _icon;
    

    private void Show(IInventoryItem item)
    {
        _name.text = item.DisplayName;
        _description.text = item.Description;
        _icon.sprite = item.Icon;

        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
