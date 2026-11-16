using System;

/// <summary>The five part slots on the gun. Each GunSocket on the gun body is one of these.</summary>
[Serializable]
public enum GunPartType
{
    Barrel,
    Magazine,
    Scope,
    Stock,
    Underbarrel
}

/// <summary>
/// How the gun fires. Stat_FireType holds it as a number, set by a barrel's Set modifier;
/// Gun.RefreshFireMode turns it into a SemiAutoFire, BurstFire, or AutoFire.
/// </summary>
public enum FireMode
{
    Semi,  // One shot per press.
    Burst, // Three quick shots per press.
    Auto   // Fires while held.
}
