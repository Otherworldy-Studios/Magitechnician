using UnityEngine;

// PLACEHOLDER.Stands in for the real consumable item until the item system exists.
// Its only job right now is to be the Source of temporary modifiers.
// TODO: replace with the real Consumable (e.g. a ScriptableObject holding its modifiers).
public sealed class Consumables
{
    public string Name { get; }

    public Consumables(string name)
    {
        Name = name;
    }

    //Readable name in logs and in EntityStatsModifier.ToString().
    public override string ToString() => Name;
}
