using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Hub shop's stock of charms and the buy rule. Not interactable yet; task 11 makes it an IInteractable
/// that opens the shop screen.
/// </summary>
public class Shop : MonoBehaviour
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

    public List<Charm> Stock => _stock;

    /// <summary>
    /// Buys a charm if it's in stock, the player has enough crystals, and the inventory takes it.
    /// The crystals are spent and the charm leaves the stock only after the inventory takes it, so a refused
    /// purchase costs nothing.
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
        _stock.Remove(charm);

        return BuyResult.Bought;
    }
}