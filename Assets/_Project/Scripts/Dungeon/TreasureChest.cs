using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

/// <summary>
/// A chest the player opens with Interact. Gives crystals and sometimes one consumable.
/// Needs a trigger collider on the Interactable layer. Room listens to OnOpened to open its portal.
/// </summary>
// Adds crystals and consumables through Inventory.AddCrystals and Inventory.TryAddItem, which update CurrencyDisplay and
// ConsumableSlotsUI through their events.
public class TreasureChest : MonoBehaviour, IInteractable
{
    [Tooltip("Shown by InteractPrompt while the player is in range.")]
    [SerializeField] private string _promptText = "Open chest";

    [Header("Contents")]
    [Tooltip("Crystals given, from X to Y, both included.")]
    [SerializeField] private Vector2Int _crystalRange = new(5, 15);

    [Tooltip("Chance of also giving one consumable, from 0 to 1.")]
    [SerializeField, Range(0f, 1f)] private float _consumableChance = 0.5f;

    [Tooltip("The consumables one is picked from, each equally likely.")]
    [SerializeField] private List<Consumable> _possibleConsumables = new();

    [Header("Visuals (optional)")]
    [SerializeField] private Animator _animator;
    [Tooltip("Animator trigger set when the chest opens.")]
    [SerializeField] private string _openTrigger = "Open";
    [Tooltip("Turned off when opened, for example the closed lid.")]
    [SerializeField] private GameObject _closedVisual;
    [Tooltip("Turned on when opened, for example the open lid.")]
    [SerializeField] private GameObject _openVisual;

    [Tooltip("For sounds and effects.")]
    [SerializeField] private UnityEvent _onOpened;

    /// <summary>Fires once when the chest opens. The chest's Room opens its portal from it.</summary>
    public event Action OnOpened;

    /// <summary>True once the chest has been opened. A chest opens only once.</summary>
    [ShowInInspector, ReadOnly]
    public bool IsOpen { get; private set; }

    /// <summary>The text InteractPrompt shows.</summary>
    public string PromptText => _promptText;

    /// <summary>True while the chest is closed and the player has an Inventory to put things in.</summary>
    public bool CanInteract(PlayerRoot player)
    {
        return !IsOpen && player != null && player.Inventory != null;
    }

    private void OnEnable()
    {
        SoundController.Instance.PlaySFX("Chest Appears", transform.position);
    }

    /// <summary>Opens the chest into the player's inventory. InteractionDetector calls this when Interact is pressed.</summary>
    public void Interact(PlayerRoot player)
    {
        if (!CanInteract(player)) return;
        Open(player.Inventory);
    }

    /// <summary>Gives the contents, plays the visuals, and fires _onOpened and OnOpened.</summary>
    private void Open(Inventory inventory)
    {
        IsOpen = true;

        int crystals = Random.Range(_crystalRange.x, _crystalRange.y + 1); // + 1 because the int Range excludes the maximum.
        if (crystals > 0)
            inventory.AddCrystals(crystals);

        Consumable consumable = null;
        bool consumableAdded = false;

        if (_possibleConsumables.Count > 0 && Random.value < _consumableChance)
        {
            consumable = _possibleConsumables[Random.Range(0, _possibleConsumables.Count)];
            if (consumable != null)
                consumableAdded = inventory.TryAddItem(consumable); // False when the consumable slots are full.
        }

        if (_animator != null && !string.IsNullOrEmpty(_openTrigger))
            _animator.SetTrigger(_openTrigger);

        if (_closedVisual != null) _closedVisual.SetActive(false);
        if (_openVisual != null) _openVisual.SetActive(true);

        _onOpened?.Invoke();
        OnOpened?.Invoke();

        if (UIManager.HasInstance)
            UIManager.Instance.ShowChestLoot(crystals, consumable, consumableAdded);
    }

    /// <summary>Debug: opens the chest for the current player without walking to it.</summary>
    [Button, DisableInEditorMode]
    private void DEBUGOpen()
    {
        GameObject player = GameManager.HasInstance ? GameManager.Instance.Player : null;
        PlayerRoot root = player != null ? player.GetComponent<PlayerRoot>() : null;
        Interact(root);
    }
}