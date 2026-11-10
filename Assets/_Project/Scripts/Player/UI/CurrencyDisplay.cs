using System;
using TMPro;
using UnityEngine;

/// <summary>Shows the player's crystal count, and updates when it changes.</summary>
// INTEGRATION: new. Listens to Inventory.OnCrystalsChanged. Connects to each player GameManager registers, because the
// HUD outlives the player between scenes.
public class CurrencyDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text _currencyText;
    [SerializeField] private Inventory _inventory; // Set from the registered player. Shown in the Inspector for debugging.

    /// <summary>Starts listening for player registration.</summary>
    private void Awake()
    {
        if (!_currencyText)
            Debug.LogError("No currencyText found in Display");

        GameManager.Instance.OnPlayerRegistered += HandlePlayerRegistered;
    }

    /// <summary>Connects to a player that registered before this existed.</summary>
    private void Start()
    {
        // If a player registered before this existed, connect to it now.
        if (GameManager.Instance.Player != null)
        {
            HandlePlayerRegistered(GameManager.Instance.Player.GetComponent<PlayerRoot>());
        }
    }

    /// <summary>Stops listening to GameManager and the player.</summary>
    private void OnDestroy()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnPlayerRegistered -= HandlePlayerRegistered;
        }
        Unbind();
    }

    /// <summary>
    /// Connects to the newly registered player. Runs on GameManager.OnPlayerRegistered, and from Start when a player was
    /// already registered.
    /// </summary>
    private void HandlePlayerRegistered(PlayerRoot player)
    {
        Unbind();

        _inventory = player.Inventory;
        if (_inventory == null)
        {
            Debug.LogError("Inventory not found for on player root!");
            return;
        }

        _currencyText.text = _inventory.Crystals.ToString();
        _inventory.OnCrystalsChanged += HandleCrystalsChanged;
    }

    /// <summary>Shows the new amount. Runs on Inventory.OnCrystalsChanged.</summary>
    private void HandleCrystalsChanged(int newAmount)
    {
        _currencyText.text = newAmount.ToString();
    }

    /// <summary>Disconnects from the current player, if any.</summary>
    private void Unbind()
    {
        if (_inventory != null)
        {
            _inventory.OnCrystalsChanged -= HandleCrystalsChanged;
        }
        _inventory = null;
    }
}
