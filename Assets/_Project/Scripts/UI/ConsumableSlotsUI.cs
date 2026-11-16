using UnityEngine;

/// <summary>
/// Shows the player's consumables in the HUD slots, in Inventory order, so slot 1 matches the 1 key.
/// Refreshes when a player registers and whenever the inventory changes.
/// </summary>
// INTEGRATION: new. Listens to Inventory.OnInventoryChanged, which Inventory.UseConsumable fires when the number key
// (InputManager.OnConsumablePressed) uses one, so the slots stay in step with the keys.
public class ConsumableSlotsUI : MonoBehaviour
{
    [Tooltip("In order. Each slot's Assigned Category must be Consumable.")]
    [SerializeField] private ItemSlotUI[] _slots;

    private Inventory _inventory; // The registered player's inventory, or null.

    /// <summary>Starts listening for player registration and connects to a player that already registered.</summary>
    // In Start instead of Awake, so GameManager exists when it's read.
    private void Start()
    {
        if (!GameManager.HasInstance) return;

        GameManager.Instance.OnPlayerRegistered += Bind;

        GameObject player = GameManager.Instance.Player;
        if (player != null) Bind(player.GetComponent<PlayerRoot>());
        else Refresh();
    }

    /// <summary>Stops listening to GameManager and the inventory.</summary>
    private void OnDestroy()
    {
        if (GameManager.HasInstance)
            GameManager.Instance.OnPlayerRegistered -= Bind;

        Unbind();
    }

    /// <summary>Connects to a player's inventory and shows its consumables. Runs on GameManager.OnPlayerRegistered.</summary>
    private void Bind(PlayerRoot player)
    {
        Unbind();

        _inventory = player != null ? player.Inventory : null;
        if (_inventory != null) _inventory.OnInventoryChanged += Refresh;

        Refresh();
    }

    /// <summary>Disconnects from the current inventory.</summary>
    private void Unbind()
    {
        if (_inventory != null) _inventory.OnInventoryChanged -= Refresh;
        _inventory = null;
    }

    /// <summary>Fills slot i with consumable i, and empties the rest.</summary>
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