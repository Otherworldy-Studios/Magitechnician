using System;
using Unity.VisualScripting;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } //singleton just in case, assumed GameManager persists between scenes
    
    public static event Action OnGameOver;
    public event Action<GameState> OnGameStateChanged;
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        Dead //game over
    }
    [SerializeField] private GameState currentState; //serialized for viewing in editor
    
    private void Awake()
    {
        Instance = this;
        currentState = GameState.Menu;
    }

    private void Start()
    {
        PH_UIManager.OnPauseRequested += TogglePause;
        //PH_RoomManager.OnRoomEntered += 
    }

    private void ChangeState(GameState newState)
    {
        if (currentState == newState)
        {
            return; //redundancy check
        }
        
        currentState = newState; //i smell a bug happening from the placement of this before the switch state but i think its overall safer ot have it before the event invokes
        
        switch (newState)
        {
            case GameState.Menu:
                HandleMenu();
                break;
            case GameState.Playing:
                HandlePlaying();
                break;
            case GameState.Dead: //contemplated whether or not dead should even be its own state (cause of pausing)
                HandleDead();
                break;
            case GameState.Paused:
                //handling pausing state is weird by its nature but here just in case
                break;
        }
        OnGameStateChanged?.Invoke(currentState); 
    }
    
    private void TogglePause()
    {
        switch (currentState)
        {
            case GameState.Playing: 
                currentState = GameState.Paused;
                Time.timeScale = 0;
                break;
            case GameState.Paused:
                currentState = GameState.Playing;
                Time.timeScale = 1;
                break;
        }
    }
    private void HandlePlaying()
    {
        //give player full control over their character n such, systems all working
    }
    private void HandleMenu()
    {
        //probably some code here about spawning back in the menu and despawning everything
    }

    private void HandleDead()
    {
        OnGameOver?.Invoke();
    }
    //track player room
    
    //generate rooms on playing, take in their room id
    
    //subscribe to room on enter event, wavemanager will probably also use that
    
    //when ui sends signal that 
    
    //stats,
    
    //on UI event call, toggle pause
    
    
}

public class PH_UIManager : MonoBehaviour
{
    public static event Action OnPauseRequested;

    public void PauseButtonPressed()
    {
        OnPauseRequested?.Invoke();
    }
}

public class PH_RoomManager : MonoBehaviour //each room cares about its own wave?
{
    public static event Action OnRoomEntered;
}