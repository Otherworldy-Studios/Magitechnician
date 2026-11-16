using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using _Project.Scripts.Dungeon;
using TMPro;

/// <summary>
/// The screen for picking one of the gun parts a cleared room rolled. Only has a Resume button so far; ShowChoices and
/// Pick are empty. Hooking it up to Room.OnRoomCleared and the loot pool is task 7.
/// </summary>
public class RewardScreen : UIScreen
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private ItemGridUI itemGrid;
    [SerializeField] private ItemSlotUI itemSlot1;
    [SerializeField] private ItemSlotUI itemSlot2;
    [SerializeField] private ItemSlotUI itemSlot3;
    protected RoomCompletionLootPool _lootPool;
    protected Gun _playerGun;
    protected Room _room;
    private int _selectedRewardIndex = -1;
    

    //item currently selected shows the text

    //send signal to players gun to give them the item

    protected override void OnShown()
    {
        //call pause
    }
    
    /// <summary>Connects the Resume button.</summary>
    private void Awake()
    {
        resumeButton.onClick.AddListener(OnResume);
        itemGrid.OnSelectionChanged += HandleSelectionChanged;
    }

    private void HandleSelectionChanged(ItemSlotUI selectedSlot)
    {
        if (_room == null || selectedSlot == null)
        {
            return;
        }


        if (selectedSlot == itemSlot1) //i would turn this into a switch if i knew how to make this static
        {
            Pick(0);
        }
        else if (selectedSlot == itemSlot2)
        {
            Pick(1);
        }
        else if (selectedSlot == itemSlot3)
        {
            Pick(2);
        }
            
    }
    /// <summary>Resume button. Closes the screen and unpauses.</summary>
    private void OnResume() //handles all the things after picking a screen
    {
        if (_selectedRewardIndex < 0) 
        {
            return;
        }
        
        bool equipped = GameManager.Instance.ChooseReward(_selectedRewardIndex);
        
        Debug.Log($"ChooseReward result: {equipped}");
        
        if (!equipped)
        {
            Debug.LogWarning("Failed to equip reward");
            return;
        }
        
        //send info to GM about selected item, GM links to player about the item
        itemSlot1.Clear();
        itemSlot2.Clear();
        itemSlot3.Clear();
        //no clear method for the grid?
        UIManager.Instance.ResumeButtonPressed(); 
    }

     /// <summary>Sets and displays all items into the item slots.</summary>
    public void ShowChoices(Room room, IReadOnlyList<WeightedReward<GunPart>> rewards) //comedically long type
    {
        _selectedRewardIndex = -1;
        _room = room;
        _lootPool = room.LootPool;
        
        //extract the items from the rewards
        IInventoryItem item1 = rewards[0].Item;
        IInventoryItem item2 = rewards[1].Item;
        IInventoryItem item3 = rewards[2].Item;
        
        //link each item slot and convert all the data
        itemSlot1.SetItem(item1); //set it in the actual UI
        itemSlot2.SetItem(item2);
        itemSlot3.SetItem(item3);
        itemGrid.Register(itemSlot1); //takes care of tooltip and selecting
        itemGrid.Register(itemSlot2);
        itemGrid.Register(itemSlot3);
        
        
        
    }

    /// <summary></summary>
    private void Pick(int index)
    {
       _selectedRewardIndex = index;
       
       Debug.Log($"Selected: {_room.RewardOptions[index].Item.PartName}");
    }
}
