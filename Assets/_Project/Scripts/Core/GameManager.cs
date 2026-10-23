using System;
using System.Collections;
using _Project.Scripts.Dungeon;
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

    public enum RunResult
    {
        Victory,
        Lose,
    }
    public static GameManager Instance { get; private set; } //assumed GameManager persists between scenes
    
    public static event Action OnGameOver;
    public static event Action OnGameWin;
    public static event Action OnGenerateRequested; 
    public static event Action OnRunStart;
    public static event Action<bool> OnPauseToggle;
    public event Action<GameState> OnGameStateChanged; 
    
    public float StartTime { get; private set; } 
    public float EndTime { get; private set; }
    public float RunTime => Time.time - StartTime; 
    
    public Room CurrentRoom { get; private set; }
    
    [SerializeField] private GameState currentState; //serialized for viewing in editor
    [SerializeField] private PH_Player playerCharacter;
    [SerializeField] private Room[,] currentDungeon;
    private bool _isPaused;
    private float _runTime;
    //should rooms carry their own timer? i think so 
    
    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        currentState = GameState.Menu;
    }

    private void Start() 
    {
        PH_UIManager.OnPauseRequested += TogglePause;
        PH_UIManager.OnResetRequested += ResetGame;
        PH_UIManager.OnStartRequested += StartHub;
        LevelGenerator.OnMapGenerated += StartRun;
        PH_Player.OnPlayerDeath += PlayerDied;
        PH_Boss.OnBossDefeated += BossDefeated;
        PH_RoomManager.OnRoomCleared += RoomCleared;

    }

    private void Update()
    {
        if (currentState == GameState.Hub && Input.GetKeyDown(KeyCode.K)) //DEBUG METHOD 
        {
            InitializeRun();
        }
    }

    public float GetRunTime()
    {
        _runTime = Time.time - StartTime; //necessary step to equalize the Time.time
        return _runTime;
    }

    public void RequestGameEnd(RunResult result)
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        switch (result)
        {
            case RunResult.Victory:
                ChangeState(GameState.Win);
                break;
            case RunResult.Lose:
                ChangeState(GameState.Dead);
                break;
        }
    }
    
    #region EventHandlers

    private void PlayerDied()  
    {
        RequestGameEnd(RunResult.Lose);
    }
    
    private void StartHub()
    {
        ChangeState(GameState.Hub);
    }
    
    private void StartRun(LevelGenerator generator) 
    {
        currentDungeon = generator.RoomGrid;
        Room startRoom = generator.StartRoom;
        CurrentRoom = startRoom;
        
        ChangeState(GameState.Playing);
        //TODO: teleport player function should be its own script?
        OnRunStart?.Invoke();
    }

    private void BossDefeated()
    {
        RequestGameEnd(RunResult.Victory);
    }
    
    private void RoomCleared()
    {
        //room itself invokes the room clear? wave manager?
        //allow the doors to open
        //rewards shou
    }
    

    #endregion
    
    private void InitializeRun() //TODO: triggered by hub system in whatever way, be it UI or portal
    {
        //DEBUG METHOD
        OnGenerateRequested?.Invoke(); 
    }

    #region StateBackbone

    private void ChangeState(GameState newState)
    {
        if (currentState == newState)
        {
            return; 
        }
    
        if (_isPaused)
        {
            TogglePause(); //unpause when quiting to menu or hub
        }
            
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
    
    private void EnterPlaying()
    {
        StartTime = Time.time; //start time is equalized to 0, showing it will be a crazy ass number cause of time.time
        //TODO: give player full control over their character n such, systems all working
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
        CaptureTime();
        OnGameOver?.Invoke();
    }
    
    private void EnterWin()
    {
        CaptureTime();
        OnGameWin?.Invoke();
    }

    #endregion
    
    #region Misc
    
    private void TogglePause()
    {
        if (currentState != GameState.Playing) 
        {
            return;
        }
    
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
        OnPauseToggle?.Invoke(_isPaused);
    }
    
    private void ResetGame()
    {
        if (currentState == GameState.Win || currentState == GameState.Dead) //only allow the reset request to work on win/loss
        {
            ChangeState(GameState.Hub); 
        }
    }
    
    #endregion

    #region Stats

    private void CaptureTime() //for the end of the run for stats
    {
        EndTime = Time.time - StartTime;
        //some extra time adding up all the room times? do this when the rooms are up
    }
    
    //EnemiesKilled
    //DamageTaken

    
    #endregion
    
    //track player room
    
}

#region PLACEHOLDERS

public class PH_UIManager : MonoBehaviour
{
    public static event Action OnPauseRequested;
    public static event Action OnStartRequested;
    public static event Action OnResetRequested;

    
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
    public static event Action OnRoomCleared;
}

public class PH_Player : MonoBehaviour
{
    public static event Action OnPlayerDeath;
    private void OnPlayerDied()
    {
        OnPlayerDeath?.Invoke();
    }
}

public class PH_Boss : MonoBehaviour
{
    public static event Action OnBossDefeated;

    private void OnBossDead()
    {
        OnBossDefeated?.Invoke();
    }
}

#endregion