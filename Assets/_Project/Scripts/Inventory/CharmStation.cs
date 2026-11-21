using UnityEngine;

/// <summary>
/// The Hub station where the player equips and unequips charms. Interacting with it opens the charm screen.
/// Needs a collider on one of InteractionDetector's Interactable Layers.
/// </summary>
public class CharmStation : MonoBehaviour, IInteractable
{
    [Tooltip("Shown by InteractPrompt while the player is in range.")]
    [SerializeField] private string _promptText = "Charms";

    public string PromptText => _promptText;

    /// <summary>True only in the Hub state. The open shop or charm screen puts the game in Store, which hides the prompt.</summary>
    public bool CanInteract(PlayerRoot player)
    {
        return GameManager.HasInstance && GameManager.Instance.CurrentState == GameManager.GameState.Hub;
    }

    public void Interact(PlayerRoot player)
    {
        UIManager.Instance.OpenCharmScreen();
    }
}