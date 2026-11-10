using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A chest the player opens with Interact.
/// Needs a trigger collider on the Interactable layer.
/// </summary>
public class TreasureChest : MonoBehaviour, IInteractable
{
    [SerializeField] private string _promptText = "Open chest";

    [Header("Contents")]
    [Tooltip("Crystals given, from X to Y, both included.")]
    [SerializeField] private Vector2Int _crystalRange = new(5, 15);

    [Tooltip("Chance of also giving one consumable, from 0 to 1.")]
    [SerializeField, Range(0f, 1f)] private float _consumableChance = 0.5f;

    [SerializeField] private List<Consumable> _possibleConsumables = new();

    [Header("Visuals (optional)")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _openTrigger = "Open";
    [Tooltip("Turned off when opened, for example the closed lid.")]
    [SerializeField] private GameObject _closedVisual;
    [Tooltip("Turned on when opened, for example the open lid.")]
    [SerializeField] private GameObject _openVisual;

    [Tooltip("For sounds and effects.")]
    public UnityEvent OnOpened;

    [ShowInInspector, ReadOnly]
    public bool IsOpen { get; private set; }

    public string PromptText => _promptText;

    public bool CanInteract(PlayerRoot player)
    {
        return !IsOpen && player != null && player.Inventory != null;
    }

    public void Interact(PlayerRoot player)
    {
        if (!CanInteract(player)) return;
        Open(player.Inventory);
    }

    private void Open(Inventory inventory)
    {
        IsOpen = true;

        int crystals = Random.Range(_crystalRange.x, _crystalRange.y + 1);
        if (crystals > 0)
            inventory.AddCrystals(crystals);

        if (_possibleConsumables.Count > 0 && Random.value < _consumableChance)
        {
            Consumable consumable = _possibleConsumables[Random.Range(0, _possibleConsumables.Count)];
            if (consumable != null && !inventory.TryAddItem(consumable))
                Debug.Log($"{name}: consumable slots are full, so {consumable.DisplayName} was skipped.", this);
        }

        if (_animator != null && !string.IsNullOrEmpty(_openTrigger))
            _animator.SetTrigger(_openTrigger);

        if (_closedVisual != null) _closedVisual.SetActive(false);
        if (_openVisual != null) _openVisual.SetActive(true);

        OnOpened?.Invoke();
    }

    [Button, DisableInEditorMode]
    private void DEBUGOpen()
    {
        GameObject player = GameManager.HasInstance ? GameManager.Instance.Player : null;
        PlayerRoot root = player != null ? player.GetComponent<PlayerRoot>() : null;
        Interact(root);
    }
}