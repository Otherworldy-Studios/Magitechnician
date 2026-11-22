using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The charm screen, where the player equips charms into charm slots. CharmStation opens it through
/// UIManager.OpenCharmScreen. Owned charms that aren't equipped go in one grid, and equipped charms in the other.
/// One button equips or unequips the selected charm through the player's Inventory.
/// </summary>
public class CharmScreen : UIScreen
{
    [Header("Charm Lists")]
    [Tooltip("Owned charms that aren't equipped.")]
    [SerializeField] private ItemGridUI _ownedGrid;
    [Tooltip("Equipped charms.")]
    [SerializeField] private ItemGridUI _equippedGrid;
    [Tooltip("One slot per charm. Set its Assigned Category to Charm.")]
    [SerializeField] private ItemSlotUI _slotPrefab;

    [Header("Buttons")]
    [Tooltip("Equips or unequips the selected charm. Its label changes to match.")]
    [SerializeField] private Button _toggleButton;
    [SerializeField] private TMP_Text _toggleButtonLabel;
    [SerializeField] private Button _closeButton;

    [Header("Text")]
    [Tooltip("How many charm slots are used, for example \"Slots: 2/3\".")]
    [SerializeField] private TMP_Text _slotsText;
    [Tooltip("Why the last equip didn't work.")]
    [SerializeField] private TMP_Text _messageText;

    // Created as needed and reused for each grid. Extra slots are turned off.
    private readonly List<ItemSlotUI> _ownedSlots = new();
    private readonly List<ItemSlotUI> _equippedSlots = new();
    private Inventory _inventory;
    private ItemSlotUI _selected;

    private void Awake()
    {
        _toggleButton.onClick.AddListener(OnToggle);
        _closeButton.onClick.AddListener(OnClose);
        _ownedGrid.OnSelectionChanged += HandleOwnedSelectionChanged;
        _equippedGrid.OnSelectionChanged += HandleEquippedSelectionChanged;
    }

    private void OnDestroy()
    {
        _toggleButton.onClick.RemoveListener(OnToggle);
        _closeButton.onClick.RemoveListener(OnClose);
        _ownedGrid.OnSelectionChanged -= HandleOwnedSelectionChanged;
        _equippedGrid.OnSelectionChanged -= HandleEquippedSelectionChanged;
        Unbind();
    }

    /// <summary>Finds the player's Inventory, then fills both grids.</summary>
    protected override void OnShown()
    {
        base.OnShown();

        Unbind();
        _inventory = FindPlayerInventory();

        if (_inventory == null)
        {
            Debug.LogError("CharmScreen found no Inventory on the registered player.", this);
        }
        else
        {
            _inventory.OnInventoryChanged += Refresh;
        }

        SetMessage("");
        Refresh();
    }

    protected override void OnHidden()
    {
        base.OnHidden();

        ClearSelection();
        Unbind();
    }

    /// <summary>Fills the owned and equipped grids, updates the slot count, and clears the selection.</summary>
    private void Refresh()
    {
        List<Charm> owned = new();
        List<Charm> equipped = new();

        if (_inventory != null)
        {
            foreach (Charm charm in _inventory.OwnedCharms)
            {
                if (_inventory.EquippedCharms.Contains(charm))
                {
                    equipped.Add(charm);
                }
                else
                {
                    owned.Add(charm);
                }
            }
        }

        FillGrid(_ownedGrid, _ownedSlots, owned);
        FillGrid(_equippedGrid, _equippedSlots, equipped);

        ClearSelection();
        //UpdateSlotsText();
    }

    /// <summary>Puts one charm in each slot, creating slots under the grid as needed, and turns off the rest.</summary>
    private void FillGrid(ItemGridUI grid, List<ItemSlotUI> slots, List<Charm> charms)
    {
        while (slots.Count < charms.Count)
        {
            ItemSlotUI slot = Instantiate(_slotPrefab, grid.transform);
            grid.Register(slot);
            slots.Add(slot);
        }

        for (int i = 0; i < slots.Count; i++)
        {
            bool hasCharm = i < charms.Count;

            if (hasCharm)
            {
                slots[i].gameObject.SetActive(true);
                slots[i].SetItem(charms[i]);
            }
            else
            {
                slots[i].Clear();
                slots[i].gameObject.SetActive(false);
            }
        }
    }

    // Selecting in one grid clears the other, so only one charm is selected on the screen.
    private void HandleOwnedSelectionChanged(ItemSlotUI slot)
    {
        if (slot != null) _equippedGrid.SelectSlot(null);
        Select(slot);
    }

    private void HandleEquippedSelectionChanged(ItemSlotUI slot)
    {
        if (slot != null) _ownedGrid.SelectSlot(null);
        Select(slot);
    }

    /// <summary>Sets the button to Equip or Unequip for the selected charm.</summary>
    private void Select(ItemSlotUI slot)
    {
        // A grid reports null when its own slot is deselected, so keep a selection the other grid made.
        if (slot == null && _selected != null && _selected.Selected) return;

        _selected = slot;
        Charm charm = SelectedCharm();

        if (charm == null)
        {
            _toggleButtonLabel.text = "Equip";
            _toggleButton.interactable = false;
            return;
        }

        bool isEquipped = _inventory.EquippedCharms.Contains(charm);

        _toggleButtonLabel.text = isEquipped ? "Unequip" : "Equip";
        _toggleButton.interactable = isEquipped || HasFreeSlot();
    }

    private void OnToggle()
    {
        Charm charm = SelectedCharm();
        if (charm == null || _inventory == null) return;

        // Both calls fire OnInventoryChanged, which refreshes the grids.
        if (_inventory.EquippedCharms.Contains(charm))
        {
            _inventory.UnequipCharm(charm);
            SetMessage("");
        }
        else if (_inventory.EquipCharm(charm))
        {
            SetMessage("");
        }
        else
        {
            SetMessage("No free charm slots.");
        }
    }

    private void OnClose()
    {
        UIManager.Instance.CloseScreen();
    }

    private Charm SelectedCharm()
    {
        return _selected != null && _inventory != null ? _selected.Item as Charm : null;
    }

    private bool HasFreeSlot()
    {
        return _inventory != null && _inventory.EquippedCharms.Count < _inventory.EquippedCharmSlots;
    }

    private void ClearSelection()
    {
        _ownedGrid.SelectSlot(null);
        _equippedGrid.SelectSlot(null);
        _selected = null;
        Select(null);
    }

    private void UpdateSlotsText()
    {
        int used = _inventory != null ? _inventory.EquippedCharms.Count : 0;
        int total = _inventory != null ? _inventory.EquippedCharmSlots : 0;
       // _slotsText.text = $"Slots: {used}/{total}";
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
            _inventory.OnInventoryChanged -= Refresh;
        }

        _inventory = null;
    }

    private static Inventory FindPlayerInventory()
    {
        if (!GameManager.HasInstance || GameManager.Instance.Player == null) return null;

        PlayerRoot root = GameManager.Instance.Player.GetComponent<PlayerRoot>();
        return root != null ? root.Inventory : null;
    }
}