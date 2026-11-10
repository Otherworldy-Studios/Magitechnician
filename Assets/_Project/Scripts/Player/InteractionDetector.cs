using System;
using UnityEngine;

/// <summary>
/// Finds the closest interactable within range of the player and uses it when Interact is pressed.
/// Put this on the player prefab. InteractPrompt reads OnFocusChanged to show or hide the prompt.
/// </summary>
public class InteractionDetector : MonoBehaviour
{
    [Tooltip("Found in parents if empty.")]
    [SerializeField] private PlayerRoot _player;

    [Tooltip("Where the range is measured from. Uses this transform if empty.")]
    [SerializeField] private Transform _origin;

    [SerializeField] private float _radius = 2f;

    [Tooltip("Set this to an Interactable layer, so the check skips the floor, walls and enemies.")]
    [SerializeField] private LayerMask _interactableLayers = ~0;

    /// <summary>The interactable the prompt is showing, or null.</summary>
    public IInteractable Current { get; private set; }

    /// <summary>Fires when Current changes. The value is null when nothing is in range.</summary>
    public event Action<IInteractable> OnFocusChanged;

    private readonly Collider[] _hits = new Collider[16];
    private bool _subscribed;

    private void Awake()
    {
        if (_player == null) _player = GetComponentInParent<PlayerRoot>();
        if (_origin == null) _origin = transform;
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        
        Subscribe();
    }

    private void OnDisable()
    {
        if (_subscribed && InputManager.HasInstance)
            InputManager.Instance.InteractPressed -= HandleInteractPressed;

        _subscribed = false;
        SetCurrent(null);
    }

    private void Update()
    {
        SetCurrent(FindClosest());
    }

    private void Subscribe()
    {
        if (_subscribed || !InputManager.HasInstance) return;

        InputManager.Instance.InteractPressed += HandleInteractPressed;
        _subscribed = true;
    }

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

            float distance = (_hits[i].bounds.center - origin).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = interactable;
            }
        }

        return closest;
    }

    private void HandleInteractPressed()
    {
        if (Current == null || IsDestroyed(Current) || !Current.CanInteract(_player)) return;

        Current.Interact(_player);
        SetCurrent(FindClosest()); 
    }

    private void SetCurrent(IInteractable interactable)
    {
        if (interactable == Current) return;

        Current = interactable;
        OnFocusChanged?.Invoke(Current);
    }

    
    private static bool IsDestroyed(IInteractable interactable)
    {
        return interactable is UnityEngine.Object unityObject && unityObject == null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_origin != null ? _origin.position : transform.position, _radius);
    }
}