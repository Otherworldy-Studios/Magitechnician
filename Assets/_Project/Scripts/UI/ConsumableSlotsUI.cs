using UnityEngine;

/// <summary>
/// Shows the player's consumables in the HUD slots, in Inventory order, so slot 1 matches the 1 key.
/// Refreshes when a player registers and whenever the inventory changes.
/// </summary>
public class ConsumableSlotsUI : MonoBehaviour
{
    [Tooltip("In order. Each slot's Assigned Category must be Consumable.")]
    [SerializeField] private ItemSlotUI[] _slots;

    private Inventory _inventory;

    private void Start()
    {
        if (!GameManager.HasInstance) return;

        GameManager.Instance.OnPlayerRegistered += Bind;

        GameObject player = GameManager.Instance.Player;
        if (player != null) Bind(player.GetComponent<PlayerRoot>());
        else Refresh();
    }

    private void OnDestroy()
    {
        if (GameManager.HasInstance)
            GameManager.Instance.OnPlayerRegistered -= Bind;

        Unbind();
    }

    private void Bind(PlayerRoot player)
    {
        Unbind();

        _inventory = player != null ? player.Inventory : null;
        if (_inventory != null) _inventory.OnInventoryChanged += Refresh;

        Refresh();
    }

    private void Unbind()
    {
        if (_inventory != null) _inventory.OnInventoryChanged -= Refresh;
        _inventory = null;
    }

    private void Refresh()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_inventory != null && i < _inventory.Consumables.Count)
                _slots[i].SetItem(_inventory.Consumables[i]);
            else
                _slots[i].Clear();
        }
    }
}