using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using _Project.Scripts.Dungeon;
using TMPro;

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
        
        
        
    }

    private void Pick(int index)
    {
        //_room.PickReward(index);
        Debug.Log($"Selected reward {index}: {_room.RewardOptions[index].Item.PartName}");
        //sends to game manager
    }

}
