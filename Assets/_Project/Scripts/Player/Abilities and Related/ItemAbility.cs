using UnityEngine;

/// <summary>
/// Base class for item abilities.
/// Subclasses should subscribe to events in OnEnable()
/// and unsubscribe from them in OnDisable().
/// </summary>
public abstract class ItemAbility : MonoBehaviour
{
    protected PlayerRoot Player { get; private set; }
    protected Gun PlayerGun { get; private set; }
    
    protected virtual void Awake()
    {
        Player = GetComponentInParent<PlayerRoot>();
        
        if (Player)
            PlayerGun = Player.Gun;
        else
            Debug.LogError("No player root found in ability", this);
    }
}