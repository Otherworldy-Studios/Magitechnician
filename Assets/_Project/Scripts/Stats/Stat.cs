using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Stat", menuName = "Scriptable Objects/Stat")]
public class Stat : ScriptableObject
{
    public float value;
    public GunStatName statName;
    public Dictionary<string, StatModifier> statModifiers = new Dictionary<string, StatModifier>();


    public void AddModifier(string modifierName, StatModifier modifier)
    {
        statModifiers[modifierName] = modifier;
    }

    public void RemoveModifier(string modifierName)
    {
        statModifiers.Remove(modifierName);
    }

    public float GetStatValue()
    {
        return this.value;
    }
}

public enum GunStatName { FireRate, FireType, BurstCount, BurstDelay, Damage, Range, BulletSpeed, Spread, MagazineSize, ReloadSpeed, AmmoType }
