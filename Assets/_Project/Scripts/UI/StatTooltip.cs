using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A small box that follows the pointer and shows a title and a description, such as a stat's. GunStatRowUI shows it
/// on hover. Stays inside its parent, so it flips to the other side of the pointer near an edge. Put it under an object
/// that covers the whole screen, such as the screen's root, and save it turned off.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class StatTooltip : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [Tooltip("Distance from the pointer to the box's corner, in Canvas units.")]
    [SerializeField] private Vector2 _offset = new(16f, -16f);

    private RectTransform _rectTransform;
    private RectTransform _parentRect;
    private Canvas _canvas;

    /// <summary>
    /// Finds the references the first time the box is used. Not in Awake, because the box is saved turned off, so Awake
    /// would only run on the first Show.
    /// </summary>
    private void EnsureInitialized()
    {
        if (_rectTransform != null) return;

        _rectTransform = (RectTransform)transform;
        _parentRect = (RectTransform)transform.parent;
        _canvas = GetComponentInParent<Canvas>(true).rootCanvas;

        
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
        }
    }

    /// <summary>Fills in the text and shows the box next to the pointer.</summary>
    /// <param name="screenPosition">The pointer's position in screen pixels.</param>
    public void Show(string title, string description, Vector2 screenPosition)
    {
        EnsureInitialized();

        _titleText.text = title;
        _descriptionText.text = description;

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        
        Canvas.ForceUpdateCanvases();
        MoveTo(screenPosition);
    }

    public void Hide()
    {
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }

    /// <summary>
    /// Places the box next to the pointer. Flips it to the left of or above the pointer when it would leave its parent.
    /// </summary>
    /// <param name="screenPosition">The pointer's position in screen pixels.</param>
    public void MoveTo(Vector2 screenPosition)
    {
        if (!gameObject.activeSelf) return;

        EnsureInitialized();

        Camera eventCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, screenPosition, eventCamera, out Vector2 local);

        Vector2 size = _rectTransform.rect.size;
        Rect bounds = _parentRect.rect;

        // Pivot (0, 1) puts the box's top left corner at the pointer plus the offset.
        float pivotX = local.x + _offset.x + size.x > bounds.xMax ? 1f : 0f;
        float pivotY = local.y + _offset.y - size.y < bounds.yMin ? 0f : 1f;
        _rectTransform.pivot = new Vector2(pivotX, pivotY);

        Vector2 offset = new Vector2(pivotX == 0f ? _offset.x : -_offset.x, pivotY == 1f ? _offset.y : -_offset.y);

        // With the anchors in the middle, the anchored position is measured from the parent's center.
        _rectTransform.anchorMin = _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _rectTransform.anchoredPosition = local - bounds.center + offset;
    }
}