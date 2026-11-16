using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// The Hub shop's stock of charms and the buy rule. Not interactable yet; task 11 makes it an IInteractable
/// that opens the shop screen.
/// </summary>
public class Shop : MonoBehaviour
{
    [Tooltip("Charms for sale.")]
    [SerializeField, FormerlySerializedAs("Stock")] private List<Charm> _stock;

    /// <summary>Charms for sale.</summary>
    public List<Charm> Stock => _stock;

    /// <summary>
    /// Buys a charm if it's in stock and the player has enough crystals. Known bugs, fixed in task 11: it never spends
    /// the crystals, and it removes the charm from stock even when the inventory refuses it.
    /// </summary>
    /// <param name="charm">The charm to buy.</param>
    /// <param name="inventory">The buyer's Inventory.</param>
    public bool TryBuy(Charm charm, Inventory inventory)
    {
        if (_stock == null) return false;
        if (_stock.Count == 0) return false;
        if (!_stock.Contains(charm)) return false;
        if (inventory == null) return false;
        if (inventory.Crystals < charm.Price) return false;

        _stock.Remove(charm);
        inventory.TryAddItem(charm);
        return true;
    }
}
