using System.Collections.Generic;

/// <summary>
/// What carries over between scenes and runs for this play session: crystals, owned charms, and which charms
/// are equipped. A plain class, not a component. GameManager creates and owns one.
/// Gun parts, consumables, health, and shield aren't saved, so each run starts fresh.
/// Saving to disk comes later.
/// </summary>
// INTEGRATION: new script. GameManager.LoadScene calls SaveFrom before the old player unloads, and
// GameManager.RegisterPlayer calls LoadInto on the next scene's player.
public class PersistentProgress
{
    // Private setters, so only SaveFrom changes them.
    public int Crystals { get; private set; }
    public List<Charm> OwnedCharms { get; private set; }
    public List<Charm> EquippedCharms { get; private set; }

    /// <summary>Starts with no crystals and no charms.</summary>
    public PersistentProgress()
    {
        Crystals = 0;
        OwnedCharms = new List<Charm>();
        EquippedCharms = new List<Charm>();
    }

    /// <summary>Copies the crystals and charm lists out of an Inventory.</summary>
    /// <param name="inventory">The Inventory of the player that's about to unload.</param>
    public void SaveFrom(Inventory inventory)
    {
        Crystals = inventory.Crystals;
        OwnedCharms = new List<Charm>(inventory.OwnedCharms);
        EquippedCharms = new List<Charm>(inventory.EquippedCharms);
    }

    /// <summary>
    /// Puts the saved crystals and charms into an Inventory. Clears its charms first, which also removes their
    /// modifiers, then adds each owned charm and equips each equipped one, which applies their modifiers again.
    /// </summary>
    /// <param name="inventory">The Inventory of the player that just registered.</param>
    public void LoadInto(Inventory inventory)
    {
        inventory.SetCrystals(Crystals); // Crystals is read-only, so the total is set through SetCrystals.
        inventory.ClearCharms();
        foreach (Charm charm in OwnedCharms)
        {
            inventory.TryAddItem(charm); // Refused past Inventory.CharmSlots, so a charm past the limit is dropped.
        }
        foreach (Charm charm in EquippedCharms)
        {
            inventory.EquipCharm(charm);
        }
    }
}
