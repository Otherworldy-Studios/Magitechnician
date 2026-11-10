using System.Collections.Generic;
using UnityEngine;

public class PersistentProgress
{
    public int Crystals;
    public List<Charm> OwnedCharms;
    public List<Charm> EquippedCharms;

    public PersistentProgress()
    {
        Crystals = 0;
        OwnedCharms = new List<Charm>();
        EquippedCharms = new List<Charm>();
    }

    public void SaveFrom(Inventory inventory)
    {
        Crystals = inventory.Crystals;
        OwnedCharms = new List<Charm>(inventory.OwnedCharms);
        EquippedCharms = new List<Charm>(inventory.EquippedCharms);
    }

    public void LoadInto(Inventory inventory)
    {
        inventory.Crystals = Crystals;
        inventory.ClearCharms();
        foreach (Charm charm in OwnedCharms)
        {
            inventory.TryAddItem(charm);
        }
        foreach (Charm charm in EquippedCharms)
        {
            inventory.EquipCharm(charm);
        }
    }
}
