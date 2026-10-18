using System;

[Serializable]
public enum GunPartType
{
    Frame,
    Barrel,
    Magazine,
    Scope,
    Stock,
    Underbarrel
}

public enum FireMode
{
    Semi,
    Burst,
    Auto
}

public enum AmmoType
{
    Light,
    Medium,
    Heavy
}
