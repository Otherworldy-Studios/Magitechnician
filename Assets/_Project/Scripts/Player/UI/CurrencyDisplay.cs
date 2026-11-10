using System;
using TMPro;
using UnityEngine;

public class CurrencyDisplay : MonoBehaviour
{
    [SerializeField] TMP_Text _currencyText;
    [SerializeField] Inventory _inventory;
    private void Awake()
    {
        if (!_currencyText)
            Debug.LogError("No currencyText found in Display");

        GameManager.Instance.OnPlayerRegistered += HandlePlayerRegistered;
    }

    private void Start()
    {
        // If a player registered before this existed, connect to it now.
        if (GameManager.Instance.Player != null)
        {
            HandlePlayerRegistered(GameManager.Instance.Player.GetComponent<PlayerRoot>());
        }

    }

    private void OnDestroy()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnPlayerRegistered -= HandlePlayerRegistered;
        }
        Unbind();
    }


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

    private void HandleCrystalsChanged(int newAmount)
    {
        _currencyText.text = newAmount.ToString();
    }

    private void Unbind()
    {
        if (_inventory != null)
        {
            _inventory.OnCrystalsChanged -= HandleCrystalsChanged;
        }
        _inventory = null;
    }

}
