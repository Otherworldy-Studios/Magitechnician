using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;

/// <summary>
/// Base class for every UI screen. UIManager shows and hides screens through Show and Hide, and sets the cursor from
/// ShowsCursor.
/// </summary>
public abstract class UIScreen : MonoBehaviour
{
    [Tooltip("Shows and frees the cursor while this screen is up.")]
    [SerializeField, FormerlySerializedAs("showsCursor")] private bool _showsCursor;

    [SerializeField] private float _fadeDuration = 0.2f;

    private Coroutine _fadeRoutine;
    private bool _hasBeenShown;
    private bool _isHiding;
    
    /// <summary>True if the cursor should be visible while this screen shows. Read by UIManager.ShowScreen.</summary>
    public bool ShowsCursor => _showsCursor;

    /// <summary>Turns the screen on and calls OnShown.</summary>
    public virtual void Show()
    {
        StopFade();

        _isHiding = false;

        CanvasGroup group = GetComponent<CanvasGroup>();

        if (group != null)
        {
            if (!gameObject.activeSelf)
            {
                group.alpha = 0f;
            }

            group.interactable = true;
            group.blocksRaycasts = true;
        }

        gameObject.SetActive(true);
        _hasBeenShown = true;

        if (group != null)
        {
            _fadeRoutine = StartCoroutine(Fade(group, 1f));
        }

        OnShown();
    }

    /// <summary>Turns the screen off and calls OnHidden.</summary>
    public virtual void Hide()
    {
        if (!gameObject.activeSelf || _isHiding)
        {
            return;
        }

        StopFade();

        CanvasGroup group = GetComponent<CanvasGroup>();

        if (group == null || !_hasBeenShown)
        {
            FinishHide();
            return;
        }

        _isHiding = true;

        group.interactable = false;
        group.blocksRaycasts = false;

        _fadeRoutine = StartCoroutine(Fade(group, 0f));
    }

    /// <summary>Runs after the screen turns on. Override to refresh what it shows.</summary>
    protected virtual void OnShown()
    {
    }

    /// <summary>Runs after the screen turns off.</summary>
    protected virtual void OnHidden()
    {
    }
    
    private IEnumerator Fade(CanvasGroup group, float target)
    {
        do
        {
            yield return null;

            group.alpha = Mathf.MoveTowards(
                group.alpha,
                target,
                Time.unscaledDeltaTime / Mathf.Max(_fadeDuration, 0.001f)
            );
        }
        while (group.alpha != target);

        _fadeRoutine = null;

        if (target == 0f)
        {
            FinishHide();
        }
    }

    private void StopFade()
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }
    }

    private void FinishHide()
    {
        _isHiding = false;
        gameObject.SetActive(false);
        OnHidden();
    }
}
