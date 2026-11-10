using UnityEngine;

/// <summary>
/// The Hub portal that starts a run. Needs a trigger collider on the Interactable layer.
/// </summary>
// INTEGRATION: new. Implements IInteractable (also new), so InteractionDetector finds it and shows its prompt.
// Calls GameManager.InitializeRun, which loads the dungeon and has LevelGenerator build it.
public class RunStartPortal : MonoBehaviour, IInteractable
{
    [SerializeField] private string _promptText = "Start run"; // Shown by InteractPrompt while the player is in range.

    /// <summary>The text InteractPrompt shows.</summary>
    public string PromptText => _promptText;

    /// <summary>True only while the game is in the Hub, so the portal can't start a second run.</summary>
    public bool CanInteract(PlayerRoot player)
    {
        return GameManager.HasInstance && GameManager.Instance.CurrentState == GameManager.GameState.Hub;
    }

    /// <summary>Starts a run. InteractionDetector calls this when Interact is pressed and CanInteract is true.</summary>
    public void Interact(PlayerRoot player)
    {
        GameManager.Instance.InitializeRun();
    }
}
