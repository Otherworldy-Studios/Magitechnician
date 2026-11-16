using System;
using UnityEngine;

/// <summary>
/// Finds the closest interactable within range of the player and uses it when Interact is pressed.
/// Put this on the player prefab. InteractPrompt reads OnFocusChanged to show or hide the prompt.
/// </summary>
// INTEGRATION: new. Works with IInteractable (new), which RunStartPortal, TreasureChest, Shop, and the charm station
// implement. Listens to InputManager.OnInteractPressed, which needed the Hold interaction removed from the Interact action.
public class InteractionDetector : MonoBehaviour
{
    [Tooltip("Found in parents if empty.")]
    [SerializeField] private PlayerRoot _player;

    [Tooltip("Where the range is measured from. Uses this transform if empty.")]
    [SerializeField] private Transform _origin;

    [Tooltip("How close an interactable must be, in meters.")]
    [SerializeField] private float _radius = 2f;

    [Tooltip("Set this to an Interactable layer, so the check skips the floor, walls and enemies.")]
    [SerializeField] private LayerMask _interactableLayers = ~0;

    /// <summary>The interactable the prompt is showing, or null.</summary>
    public IInteractable Current { get; private set; }

    /// <summary>Fires when Current changes. The value is null when nothing is in range.</summary>
    public event Action<IInteractable> OnFocusChanged;

    private readonly Collider[] _hits = new Collider[16]; // Buffer for OverlapSphereNonAlloc.
    private bool _subscribed; // True while listening to InputManager, so the handler is never added twice.

    /// <summary>Finds the PlayerRoot and defaults the origin to this transform.</summary>
    private void Awake()
    {
        if (_player == null) _player = GetComponentInParent<PlayerRoot>();
        if (_origin == null) _origin = transform;
    }

    /// <summary>Starts listening for Interact.</summary>
    private void OnEnable()
    {
        Subscribe();
    }

    /// <summary>Tries again in Start, in case InputManager didn't exist yet in OnEnable.</summary>
    private void Start()
    {
        Subscribe();
    }

    /// <summary>Stops listening and clears the prompt.</summary>
    private void OnDisable()
    {
        if (_subscribed && InputManager.HasInstance)
            InputManager.Instance.OnInteractPressed -= HandleInteractPressed;

        _subscribed = false;
        SetCurrent(null);
    }

    /// <summary>Picks the closest interactable every frame.</summary>
    private void Update()
    {
        SetCurrent(FindClosest());
    }

    /// <summary>Listens to InputManager.OnInteractPressed, once, if InputManager exists.</summary>
    private void Subscribe()
    {
        if (_subscribed || !InputManager.HasInstance) return;

        InputManager.Instance.OnInteractPressed += HandleInteractPressed;
        _subscribed = true;
    }

    /// <summary>The closest interactable in range whose CanInteract is true, or null.</summary>
    private IInteractable FindClosest()
    {
        Vector3 origin = _origin.position;
        int count = Physics.OverlapSphereNonAlloc(origin, _radius, _hits, _interactableLayers, QueryTriggerInteraction.Collide);

        IInteractable closest = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            IInteractable interactable = _hits[i].GetComponentInParent<IInteractable>();
            if (interactable == null || !interactable.CanInteract(_player)) continue;

            float distance = (_hits[i].bounds.center - origin).sqrMagnitude; // Squared, which compares the same and skips a square root.
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = interactable;
            }
        }

        return closest;
    }

    /// <summary>Uses the current interactable, then looks again, because using it can change what's in range.</summary>
    private void HandleInteractPressed()
    {
        if (Current == null || IsDestroyed(Current) || !Current.CanInteract(_player)) return;

        Current.Interact(_player);
        SetCurrent(FindClosest());
    }

    /// <summary>Changes Current and fires OnFocusChanged, only when it's different.</summary>
    private void SetCurrent(IInteractable interactable)
    {
        if (interactable == Current) return;

        Current = interactable;
        OnFocusChanged?.Invoke(Current);
    }

    /// <summary>
    /// True for a destroyed Unity object. An interface reference to one isn't null in C#, so it's checked as a Unity Object.
    /// </summary>
    private static bool IsDestroyed(IInteractable interactable)
    {
        return interactable is UnityEngine.Object unityObject && unityObject == null;
    }

    /// <summary>Draws the interact range in the Scene view.</summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_origin != null ? _origin.position : transform.position, _radius);
    }
}