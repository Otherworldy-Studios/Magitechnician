using UnityEngine;

/// <summary>
/// The Hub portal that starts a run. Needs a trigger collider on the Interactable layer.
/// </summary>
public class RunStartPortal : MonoBehaviour, IInteractable
{
    [SerializeField] private string _promptText = "Start run";

    public string PromptText => _promptText;

    // Only in the Hub
    public bool CanInteract(PlayerRoot player)
    {
        return GameManager.HasInstance && GameManager.Instance.CurrentState == GameManager.GameState.Hub;
    }

    public void Interact(PlayerRoot player)
    {
        GameManager.Instance.InitializeRun();
    }
}