using System;
using UnityEngine;

/// <summary>
/// Base class for item abilities.
/// Subclasses should subscribe to events in OnEnable()
/// and unsubscribe from them in OnDisable().
/// Subclasses call ReportActivated() each time the ability does its effect.
/// </summary>
public abstract class ItemAbility : MonoBehaviour
{
    /// <summary>Fires each time this ability does its effect. PlayerAbilities passes it on as OnAbilityActivated.</summary>
    public event Action<ItemAbility> OnActivated;

    protected PlayerRoot Player { get; private set; }
    protected Gun PlayerGun { get; private set; }

    /// <summary>The charm, gun part, or consumable that granted this ability. PlayerAbilities sets it.</summary>
    public IInventoryItem SourceItem { get; private set; }

    protected virtual void Awake()
    {
        Player = GetComponentInParent<PlayerRoot>();

        if (Player)
            PlayerGun = Player.Gun;
        else
            Debug.LogError("No player root found in ability", this);
    }

    public void SetSourceItem(IInventoryItem item)
    {
        SourceItem = item;
    }

    protected void ReportActivated()
    {
        OnActivated?.Invoke(this);
    }
}