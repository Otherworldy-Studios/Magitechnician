using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The screen for picking one of the gun parts a cleared room rolled. UIManager.HandleRewards opens it and calls
/// ShowChoices. Resume equips the selected part through GameManager.ChooseReward and closes the screen.
/// If the part can't be equipped, the screen shows why, and pressing Resume again closes it without a part.
/// </summary>
public class RewardScreen : UIScreen
{
    [Header("References")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private ItemGridUI _itemGrid;
    [SerializeField] private ItemSlotUI _itemSlot1;
    [SerializeField] private ItemSlotUI _itemSlot2;
    [SerializeField] private ItemSlotUI _itemSlot3;
    [Tooltip("Shows why Resume didn't close the screen. Hidden until needed.")]
    [SerializeField] private TMP_Text _errorText;

    private const string NoSelectionMessage = "Pick a part first.";

    private ItemSlotUI[] _itemSlots;
    private IReadOnlyList<WeightedReward<GunPart>> _rewards;
    private int _selectedRewardIndex = -1;
    private bool _equipFailed; // True after ChooseReward fails, so the next Resume closes without a part.

    private void Awake()
    {
        _itemSlots = new[] { _itemSlot1, _itemSlot2, _itemSlot3 };

        _resumeButton.onClick.AddListener(OnResume);
        _itemGrid.OnSelectionChanged += HandleSelectionChanged;
    }

    private void OnDestroy()
    {
        _resumeButton.onClick.RemoveListener(OnResume);
        _itemGrid.OnSelectionChanged -= HandleSelectionChanged;
    }

    /// <summary>Sets and displays all items into the item slots.</summary>
    public void ShowChoices(IReadOnlyList<WeightedReward<GunPart>> rewards)
    {
        _rewards = rewards;
        _selectedRewardIndex = -1;
        _equipFailed = false;
        HideError();

        for (int i = 0; i < _itemSlots.Length; i++)
        {
            bool hasReward = i < rewards.Count;
            _itemSlots[i].gameObject.SetActive(hasReward);

            if (hasReward)
            {
                _itemSlots[i].SetItem(rewards[i].Item);
            }
        }
    }

    private void HandleSelectionChanged(ItemSlotUI selectedSlot)
    {
        if (_rewards == null || selectedSlot == null) return;

        int index = Array.IndexOf(_itemSlots, selectedSlot);
        if (index >= 0 && index < _rewards.Count)
        {
            Pick(index);
        }
    }

    /// <summary>Selects a part. Picking a new part after a failed equip lets Resume try that part instead.</summary>
    private void Pick(int index)
    {
        _selectedRewardIndex = index;
        _equipFailed = false;
        HideError();
    }

    private void OnResume()
    {
        if (_equipFailed)
        {
            GameManager.Instance.SkipReward();
            Close();
            return;
        }

        if (_selectedRewardIndex < 0)
        {
            ShowError(NoSelectionMessage);
            return;
        }

        if (!GameManager.Instance.ChooseReward(_selectedRewardIndex))
        {
            _equipFailed = true;
            string partName = _rewards[_selectedRewardIndex].Item.PartName;
            ShowError($"Couldn't equip {partName}. Press Resume to continue without it.");
            return;
        }

        Close();
    }

    /// <summary>Empties the slots, hides the error, and hands control back to the game.</summary>
    private void Close()
    {
        foreach (ItemSlotUI slot in _itemSlots)
        {
            slot.Clear();
        }

        _rewards = null;
        _selectedRewardIndex = -1;
        _equipFailed = false;
        HideError();

        UIManager.Instance.ResumeButtonPressed();
    }

    private void ShowError(string message)
    {
        if (_errorText == null) return;

        _errorText.text = message;
        _errorText.gameObject.SetActive(true);
    }

    private void HideError()
    {
        if (_errorText == null) return;

        _errorText.gameObject.SetActive(false);
    }
}