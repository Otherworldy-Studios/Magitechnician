using UnityEngine.UI;
using UnityEngine;

public class RewardScreen : UIScreen
{
    [SerializeField] private Button resumeButton;
    protected RoomCompletionLootPool _lootPool;
    protected Gun _playerGun;


    private void Awake()
    {
        resumeButton.onClick.AddListener(OnResume);
    }

    private void OnResume()
    {
        UIManager.Instance.ResumeButtonPressed(); //OR pressing escape again but this should be implicit from the GM
    }

    public void ShowChoices()
    {
        
    }

    private void Pick(int index)
    {
        
    }

}
