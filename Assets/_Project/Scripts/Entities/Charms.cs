using System.Collections.Generic;
using UnityEngine;

//PLACEHOLDER.Stands in for the real charm item until the item system exists.
//Its only job right now is to be the Source of permanent modifiers, so that
//unequipping it (Stats.RemoveAllFromSource) removes only its modifiers.
//TODO: replace with the real Charm (e.g. a ScriptableObject holding its modifiers).
public sealed class Charms 
{
    public string Name { get; }

    public Charms(string name)
    {
        Name = name; 
    }


    //Readable name in logs and in EntityStatsModifier.ToString().
    public override string ToString() => Name;
}
