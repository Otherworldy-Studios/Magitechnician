using System;
using Unity.VisualScripting;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Menu,
        Hub,
        Playing,
        Dead, //game over
        Win
    }
    public static GameManager Instance { get; private set; } //singleton just in case, assumed GameManager persists between scenes
    
    public static event Action OnGameOver;
    public static event Action OnGameWin;
    public static event Action OnGenerateRequested;
    public static event Action OnRunStart;
    public event Action<GameState> OnGameStateChanged;
    
    
    public float RunTime { get; private set; }
    public float RoomTime { get; private set; } //should rooms carry their own timer?

    [SerializeField] private GameState currentState; //serialized for viewing in editor
    
    private float _startTime; //helper variable to find other times

    private bool _isPaused;
    
    
    private void Awake()
    {
        Instance = this;
        currentState = GameState.Menu;
    }

    private void Start()
    {
        PH_UIManager.OnPauseRequested += TogglePause;
        PH_DungeonManager.OnGenerate += StartRun;
    }

    private void ChangeState(GameState newState)
    {
        if (currentState == newState)
        {
            return; 
        }

        //currently no exit state switch but no use for it right now
        
        currentState = newState; 
        
        switch (newState)
        {
            case GameState.Menu:
                EnterMenu();
                break;
            case GameState.Hub:
                EnterHub();
                break;
            case GameState.Playing:
                EnterPlaying();
                break;
            case GameState.Dead:
                EnterDead();
                break;
            case GameState.Win:
                EnterWin();
                break;
        }
        OnGameStateChanged?.Invoke(currentState); 
    }

    private void InitializeRun() //generation of layout, and helps move towards playing
    {
        
        //ask to generate run
        //move player over to the first room
        OnGenerateRequested?.Invoke();
        
        
    }

    private void StartRun()
    {
        OnRunStart?.Invoke();
        ChangeState(GameState.Playing);
        _startTime = 0;
    }
    
    private void TogglePause() //separate pause state method needed for timescale order funkyness
    {
        if (_isPaused)
        {
            Time.timeScale = 1;
            _isPaused = false;
        }
        else
        {
            Time.timeScale = 0;
            _isPaused = true;
        }
    }
    private void EnterPlaying()
    {
        //start a timer based off Time.time for the stat
        //give player full control over their character n such, systems all working
    }
    private void EnterMenu()
    {
        //probably some code here about spawning back in the menu and despawning everything
    }

    private void EnterHub()
    {
        
    }

    private void EnterDead()
    {
        OnGameOver?.Invoke();
    }

    private void EnterWin()
    {
        OnGameWin?.Invoke();
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
    public static event Action OnStartRequested;

    
    private void Start()
    {
        GameManager.OnGameOver += GameOver;
        GameManager.OnGameWin += GameWin;
    }
    
    
    private void PauseButtonPressed()
    {
        OnPauseRequested?.Invoke();
    }

    public void StartButtonPressed()
    {
        OnStartRequested?.Invoke();
    }

    private void GameOver()
    {
        
    }

    private void GameWin()
    {
        
    }
    
}

public class PH_RoomManager : MonoBehaviour //each room cares about its own wave?
{
    public static event Action OnRoomEntered;
}

public class PH_DungeonManager : MonoBehaviour
{
    
}

public class PH_HubManager : MonoBehaviour //unsure if this needs to be a future thing, im currently assuming multiple systems would like to know whether someones in
{
    
}