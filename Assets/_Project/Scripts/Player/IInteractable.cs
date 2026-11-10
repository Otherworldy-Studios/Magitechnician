/// <summary>
/// Anything the player can use with the Interact button: portals, chests, the shop, the charm station.
/// </summary>
// INTEGRATION: new. InteractionDetector finds these and InteractPrompt shows their PromptText.
public interface IInteractable
{
    /// <summary>The action shown after the key in the prompt, for example "Start run" or "Open chest".</summary>
    string PromptText { get; }

    /// <summary>False hides the prompt and ignores the button, for example on a chest that's already open.</summary>
    bool CanInteract(PlayerRoot player);

    /// <summary>Runs when the player presses Interact while this is the closest interactable.</summary>
    void Interact(PlayerRoot player);
}