using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Hub shop screen, where the player buys charms with crystals. UIManager.OpenShop shows it. Lists the charms
/// the player doesn't own yet, one ItemSlotUI per charm, and buys the selected one through Shop.TryBuy.
/// </summary>
public class ShopScreen : UIScreen
{
    [Header("Charm List")]
    [Tooltip("The grid the charm slots are created under. Its tooltip is shared by every slot.")]
    [SerializeField] private ItemGridUI _grid;
    [Tooltip("One slot per charm. Set its Assigned Category to Charm.")]
    [SerializeField] private ItemSlotUI _slotPrefab;

    [Header("Buttons")]
    [SerializeField] private Button _buyButton;
    [Tooltip("Shows the selected charm's price, for example \"Buy: 20 crystals\".")]
    [SerializeField] private TMP_Text _buyButtonLabel;
    [SerializeField] private Button _closeButton;

    [Header("Text")]
    [Tooltip("The player's crystals.")]
    [SerializeField] private TMP_Text _crystalsText;
    [Tooltip("What happened on the last purchase.")]
    [SerializeField] private TMP_Text _messageText;
    [Tooltip("The selected charm's price. Empty when nothing is selected.")]
    [SerializeField] private TMP_Text _priceText;

    private readonly List<ItemSlotUI> _slots = new(); // Created as needed and reused. Extra slots are turned off.
    private Shop _shop;
    private Inventory _inventory;

    private void Awake()
    {
        _buyButton.onClick.AddListener(OnBuy);
        _closeButton.onClick.AddListener(OnClose);
        _grid.OnSelectionChanged += HandleSelectionChanged;
    }

    private void OnDestroy()
    {
        _buyButton.onClick.RemoveListener(OnBuy);
        _closeButton.onClick.RemoveListener(OnClose);
        _grid.OnSelectionChanged -= HandleSelectionChanged;
        Unbind();
    }

    /// <summary>Finds the Hub's Shop and the player's Inventory, then fills the list.</summary>
    protected override void OnShown()
    {
        base.OnShown();

        Unbind();
        _shop = FindFirstObjectByType<Shop>();
        _inventory = FindPlayerInventory();

        if (_shop == null) Debug.LogError("ShopScreen found no Shop in the scene.", this);
        if (_inventory == null) Debug.LogError("ShopScreen found no Inventory on the registered player.", this);

        if (_inventory != null)
        {
            _inventory.OnCrystalsChanged += HandleCrystalsChanged;
        }

        SetMessage("");
        Refresh();
    }

    protected override void OnHidden()
    {
        base.OnHidden();

        _grid.SelectSlot(null);
        Unbind();
    }

    /// <summary>Fills one slot per charm the player can buy, turns off the rest, and clears the selection.</summary>
    private void Refresh()
    {
        List<Charm> available = _shop != null ? _shop.GetAvailable(_inventory) : new List<Charm>();

        for (int i = 0; i < available.Count; i++)
        {
            ItemSlotUI slot = GetSlot(i);
            slot.gameObject.SetActive(true);
            slot.SetItem(available[i]);
        }

        for (int i = available.Count; i < _slots.Count; i++)
        {
            _slots[i].Clear();
            _slots[i].gameObject.SetActive(false);
        }

        _grid.SelectSlot(null);
        HandleSelectionChanged(null);
        UpdateCrystals();
    }

    /// <summary>Returns the slot at the index, creating it under the grid if it doesn't exist yet.</summary>
    private ItemSlotUI GetSlot(int index)
    {
        while (_slots.Count <= index)
        {
            ItemSlotUI slot = Instantiate(_slotPrefab, _grid.transform);
            _grid.Register(slot);
            _slots.Add(slot);
        }

        return _slots[index];
    }

    /// <summary>
    /// Shows the selected charm's price in Price Text and on the Buy button, and allows Buy only when the player can
    /// afford it. Clears the price when nothing is selected.
    /// </summary>
    private void HandleSelectionChanged(ItemSlotUI slot)
    {
        Charm charm = slot != null ? slot.Item as Charm : null;

        if (charm == null)
        {
            SetPrice("");
            _buyButtonLabel.text = "Buy";
            _buyButton.interactable = false;
            return;
        }

        SetPrice(charm.Price.ToString());
        _buyButtonLabel.text = $"Buy: {charm.Price} crystals";
        _buyButton.interactable = _inventory != null && _inventory.Crystals >= charm.Price;
    }

    private void HandleCrystalsChanged(int crystals)
    {
        UpdateCrystals();
        HandleSelectionChanged(_grid.Selected);
    }

    private void OnBuy()
    {
        Charm charm = _grid.Selected != null ? _grid.Selected.Item as Charm : null;
        if (charm == null || _shop == null) return;

        Shop.BuyResult result = _shop.TryBuy(charm, _inventory);

        switch (result)
        {
            case Shop.BuyResult.Bought:
                SoundController.Instance.PlayUI("UI Buy Confirm");
                SetMessage($"Bought {charm.DisplayName}.");
                Refresh();
                break;
            case Shop.BuyResult.NotEnoughCrystals:
                SetMessage("Not enough crystals.");
                break;
            case Shop.BuyResult.NoRoom:
                SetMessage("You can't carry that charm.");
                break;
            case Shop.BuyResult.NotInStock:
                SetMessage("That charm is sold out.");
                Refresh();
                break;
        }
    }

    private void OnClose()
    {
        UIManager.Instance.CloseScreen();
    }

    private void UpdateCrystals()
    {
        int crystals = _inventory != null ? _inventory.Crystals : 0;
        _crystalsText.text = $"Crystals: {crystals}";
    }

    private void SetPrice(string price)
    {
        if (_priceText != null)
        {
            _priceText.text = price;
        }
    }

    private void SetMessage(string message)
    {
        if (_messageText != null)
        {
            _messageText.text = message;
        }
    }

    private void Unbind()
    {
        if (_inventory != null)
        {
            _inventory.OnCrystalsChanged -= HandleCrystalsChanged;
        }

        _inventory = null;
        _shop = null;
    }

    private static Inventory FindPlayerInventory()
    {
        if (!GameManager.HasInstance || GameManager.Instance.Player == null) return null;

        PlayerRoot root = GameManager.Instance.Player.GetComponent<PlayerRoot>();
        return root != null ? root.Inventory : null;
    }
}