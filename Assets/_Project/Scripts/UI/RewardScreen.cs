using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using _Project.Scripts.Dungeon;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

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
    
    

    //item currently selected shows the text

    //send signal to players gun to give them the item
    
    [SerializeField, FormerlySerializedAs("resumeButton")] private Button _resumeButton;

    protected RoomCompletionLootPool _lootPool; // The pool the shown parts came from. Not set yet.
    protected Gun _playerGun;                   // The gun the picked part goes on. Not set yet.

    /// <summary>Connects the Resume button.</summary>
    private void Awake()
    {
        _resumeButton.onClick.AddListener(OnResume);
    }

    private void OnResume() //handles all the things after picking a screen
    {
        //send info to GM about selected item, GM links to player about the item
        itemSlot1.Clear();
        itemSlot2.Clear();
        itemSlot3.Clear();
        //no clear method for the grid?
        UIManager.Instance.ResumeButtonPressed(); 
    }

    public void ShowChoices(Room room, IReadOnlyList<WeightedReward<GunPart>> rewards) //comedically long type
    {
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
        
        
        
    /// <summary>Resume button. Closes the screen and unpauses.</summary>
    private void OnResume()
    {
        UIManager.Instance.ResumeButtonPressed();
    }

    /// <summary>Will show the rolled parts. Empty until task 7.</summary>
    public void ShowChoices()
    {
    }

    /// <summary>Will equip the chosen part and return the others to the pool. Empty until task 7.</summary>
    private void Pick(int index)
    {
        //_room.PickReward(index);
        
    }
}
