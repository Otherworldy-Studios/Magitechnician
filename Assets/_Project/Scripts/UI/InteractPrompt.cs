using TMPro;
using UnityEngine;

/// <summary>
/// Shows "E: Start run" while the player is in range of an interactable. Put this on the HUD.
///
/// Put the script on an object that stays active (for example the HUD), and point Prompt Root
/// at the child panel to show and hide.
/// </summary>
// INTEGRATION: new. Listens to InteractionDetector.OnFocusChanged (new) on each player GameManager registers, and shows
// the IInteractable's PromptText.
public class InteractPrompt : MonoBehaviour
{
    [Tooltip("The panel shown while something is in range. Must be a child, not this object.")]
    [SerializeField] private GameObject _promptRoot;

    [SerializeField] private TMP_Text _label; // Shows the key and the action.

    [Tooltip("Shown before the action. Match the Interact binding.")]
    [SerializeField] private string _keyLabel = "E";

    private InteractionDetector _detector; // On the registered player, or null.

    /// <summary>Hides the prompt, starts listening for player registration, and connects to a player already registered.</summary>
    private void Start()
    {
        Show(null);

        if (!GameManager.HasInstance) return;

        GameManager.Instance.OnPlayerRegistered += Bind;

        // The player may have registered before this Start ran.
        GameObject player = GameManager.Instance.Player;
        if (player != null)
            Bind(player.GetComponent<PlayerRoot>());
    }

    /// <summary>Stops listening to GameManager and the detector.</summary>
    private void OnDestroy()
    {
        if (GameManager.HasInstance)
            GameManager.Instance.OnPlayerRegistered -= Bind;

        Unbind();
    }

    /// <summary>Connects to the player's InteractionDetector. Runs on GameManager.OnPlayerRegistered.</summary>
    private void Bind(PlayerRoot player)
    {
        Unbind();

        if (player == null) return;

        _detector = player.GetComponentInChildren<InteractionDetector>();
        if (_detector == null) return;

        _detector.OnFocusChanged += Show;
        Show(_detector.Current);
    }

    /// <summary>Disconnects from the current detector.</summary>
    private void Unbind()
    {
        if (_detector != null)
            _detector.OnFocusChanged -= Show;

        _detector = null;
    }

    /// <summary>Shows the prompt for an interactable, or hides it for null. Runs on InteractionDetector.OnFocusChanged.</summary>
    private void Show(IInteractable interactable)
    {
        bool visible = interactable != null;

        if (_promptRoot != null)
            _promptRoot.SetActive(visible);

        if (visible && _label != null)
            _label.text = $"{_keyLabel}: {interactable.PromptText}";
    }
}