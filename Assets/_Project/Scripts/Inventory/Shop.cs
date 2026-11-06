using System.Collections.Generic;
using UnityEngine;

public class Shop : MonoBehaviour
{
    public List<Charm> Stock;

    public bool TryBuy(Charm charm, Inventory inventory)
    {
        if(Stock == null) return false;
        if(Stock.Count == 0) return false;
        if (!Stock.Contains(charm)) return false;
        if(inventory == null) return false;
        if (inventory.Crystals < charm.Price) return false;

        Stock.Remove(charm);
        inventory.TryAddItem(charm);
        return true;
    }

}
