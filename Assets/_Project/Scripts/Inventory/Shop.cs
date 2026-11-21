using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The Hub shop's stock of charms and the buy rule. Interacting with it opens the shop screen.
/// Needs a collider on one of InteractionDetector's Interactable Layers.
/// </summary>
public class Shop : MonoBehaviour, IInteractable
{
    /// <summary>How a purchase went. ShopScreen picks its message from this.</summary>
    public enum BuyResult
    {
        Bought,
        NotInStock,
        NotEnoughCrystals,
        NoRoom,
    }

    [Tooltip("Charms for sale.")]
    [SerializeField] private List<Charm> _stock;
    [Tooltip("Shown by InteractPrompt while the player is in range.")]
    [SerializeField] private string _promptText = "Shop";

    public List<Charm> Stock => _stock;
    public string PromptText => _promptText;

    /// <summary>True only in the Hub state. The open shop or charm screen puts the game in Store, which hides the prompt.</summary>
    public bool CanInteract(PlayerRoot player)
    {
        return GameManager.HasInstance && GameManager.Instance.CurrentState == GameManager.GameState.Hub;
    }

    public void Interact(PlayerRoot player)
    {
        UIManager.Instance.OpenShop();
    }

    /// <summary>The charms in stock that the inventory doesn't own yet. ShopScreen lists these.</summary>
    public List<Charm> GetAvailable(Inventory inventory)
    {
        List<Charm> available = new();
        if (_stock == null) return available;

        foreach (Charm charm in _stock)
        {
            if (charm == null) continue;
            if (inventory != null && inventory.OwnedCharms.Contains(charm)) continue;

            available.Add(charm);
        }

        return available;
    }

    /// <summary>
    /// Buys a charm if it's in stock, the player has enough crystals, and the inventory takes it.
    /// The crystals are spent only after the inventory takes the charm
    /// The charm stays in Stock, and GetAvailable hides it once it's owned.
    /// </summary>
    /// <param name="charm">The charm to buy.</param>
    /// <param name="inventory">The buyer's Inventory.</param>
    public BuyResult TryBuy(Charm charm, Inventory inventory)
    {
        if (charm == null || _stock == null || !_stock.Contains(charm)) return BuyResult.NotInStock;

        if (inventory == null)
        {
            Debug.LogError("Shop.TryBuy was called without an Inventory.", this);
            return BuyResult.NoRoom;
        }

        if (inventory.Crystals < charm.Price) return BuyResult.NotEnoughCrystals;

        if (!inventory.TryAddItem(charm)) return BuyResult.NoRoom;

        // Can't fail, because the crystals were checked above.
        inventory.TrySpendCrystals(charm.Price);

        return BuyResult.Bought;
    }
}