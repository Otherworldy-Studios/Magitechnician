using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Base class for every UI screen. UIManager shows and hides screens through Show and Hide, and sets the cursor from
/// ShowsCursor.
/// </summary>
public abstract class UIScreen : MonoBehaviour
{
    [Tooltip("Shows and frees the cursor while this screen is up.")]
    [SerializeField, FormerlySerializedAs("showsCursor")] private bool _showsCursor;

    /// <summary>True if the cursor should be visible while this screen shows. Read by UIManager.ShowScreen.</summary>
    public bool ShowsCursor => _showsCursor;

    /// <summary>Turns the screen on and calls OnShown.</summary>
    public virtual void Show()
    {
        gameObject.SetActive(true);
        OnShown();
    }

    /// <summary>Turns the screen off and calls OnHidden.</summary>
    public virtual void Hide()
    {
        gameObject.SetActive(false);
        OnHidden();
    }

    /// <summary>Runs after the screen turns on. Override to refresh what it shows.</summary>
    protected virtual void OnShown()
    {
    }

    /// <summary>Runs after the screen turns off.</summary>
    protected virtual void OnHidden()
    {
    }
}
