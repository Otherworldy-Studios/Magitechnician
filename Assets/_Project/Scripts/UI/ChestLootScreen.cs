using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows what a treasure chest gave the player. TreasureChest opens it through UIManager.ShowChestLoot, and Continue
/// closes it and unpauses the game.
/// </summary>
public class ChestLootScreen : UIScreen
{
    [Header("References")]
    [SerializeField] private Button _continueButton;
    [SerializeField] private TMP_Text _crystalsText;
    [Tooltip("Shows the consumable's name, description, and icon. Hidden when the chest gave no consumable.")]
    [SerializeField] private ItemTooltip _itemTooltip;
    [Tooltip("Shown when the chest had a consumable but the consumable slots were full. Optional.")]
    [SerializeField] private GameObject _slotsFullText;

    private void Awake()
    {
        _continueButton.onClick.AddListener(Close);
    }

    private void OnDestroy()
    {
        _continueButton.onClick.RemoveListener(Close);
    }

    public void ShowLoot(int crystals, Consumable consumable, bool consumableAdded)
    {
        _crystalsText.gameObject.SetActive(crystals > 0);
        _crystalsText.text = $"+{crystals} Crystals";

        bool hasConsumable = consumable != null;
        _itemTooltip.gameObject.SetActive(hasConsumable);

        if (hasConsumable)
            _itemTooltip.Show(consumable, this);
        else
            _itemTooltip.Clear();

        if (_slotsFullText != null)
            _slotsFullText.SetActive(hasConsumable && !consumableAdded);
    }

    private void Close()
    {
        UIManager.Instance.ResumeButtonPressed();
    }
}