/// <summary>How a StatModifier changes its stat. Stats.Recalculate applies them as (base + Adds) × Multiplies, unless a Set is active.</summary>
public enum ModifierOperation
{
    Add,      // Adds Value to the base.
    Multiply, // Multiplies the total by Value. +20% is 1.2, and -25% is 0.75.
    Set       // INTEGRATION: new, added last so existing modifier assets keep their operation. Replaces the final value;
              // the most recently added Set wins. Gun.RefreshFireMode reads FireType this way, for barrels that set a fire mode.
}
