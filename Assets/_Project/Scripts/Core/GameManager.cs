using System;
using System.Collections;
using System.Linq.Expressions;
using _Project.Scripts.Dungeon;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Menu, //menu on bootup
        Hub, //pre-run area
        Playing, //playing game
        Store, 
        Dead, //game over
        Win
    }

    public enum RunResult
    {
        Victory,
        Lose,
    }
    public static GameManager Instance { get; private set; } 
    
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
    [SerializeField] private PH_WaveManager waveManager; //wave manager responsible for spawning in enemies?
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
        PH_WaveManager.OnRoomCleared += RoomCleared;
    }

    private void Update()
    {
        DEBUGInitializeRun();
    }

    private void DEBUGInitializeRun()
    {
        if (currentState == GameState.Hub && Input.GetKeyDown(KeyCode.K)) 
        {
            InitializeRun();
        }
    }
    public void ChangeRoom(Room newRoom)
    {
        CurrentRoom = newRoom;
    }
    
    /// <summary>
    /// any necessary source may request the GM to end the game while giving the desired result, validates if a run should actually end then simply switches to the state
    /// </summary>
    /// <param name="result">Victory or Loss; whether the game should end in which.</param>
    public void RequestGameEnd(RunResult result)
    {
        if (currentState != GameState.Playing && currentState != GameState.Store) //only allow to happen while a run is going on
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

    public void GoTo(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
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
    
    /// <summary>
    /// happens after the generation request to the level gen is completed,
    /// fills in necessary variables for tracking the dungeon here
    /// and handles the first swap to playing state for that dungeon (otherwise bugs between playing states)
    /// </summary>
    /// <param name="generator">The level generator object that contains the dungeon</param>
    private void StartRun(LevelGenerator generator) 
    {
        currentDungeon = generator.RoomGrid;
        Room startRoom = generator.StartRoom;
        CurrentRoom = startRoom;
        
        ChangeState(GameState.Playing);
        StartTime = Time.time; //start time is now the "0" that other times will be equalized to
        //TODO: teleport player function should be its own script maybe? reusable for the enemy spawns maybe?
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
    
    /// <summary>
    /// Starts the request process for starting a run 
    /// </summary>
    private void InitializeRun() //TODO: triggered by hub system in whatever way, be it UI or hub portal
    {
        OnGenerateRequested?.Invoke(); 
    }

    #endregion
    
    

    #region StateManagement
    
    /// <summary>
    /// Checks if the newState is even different and denies otherwise, and handles unpausing for entering certain states for the pause menu
    /// switches and calls enter(state) functions
    /// </summary>
    /// <param name="newState">The state to transition into.</param>
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
            case GameState.Store:
                EnterStore();
                break;
        }
        OnGameStateChanged?.Invoke(currentState); 
    }
    
    private void EnterPlaying()
    {
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

    private void EnterStore()
    {
        
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

    /// <summary>
    /// End of run captures all total time
    /// </summary>
    private void CaptureTime() 
    {
        EndTime = Time.time - StartTime;
        //TODO: some extra time adding up all the room times? do this when the rooms are up
    }
    
    //EnemiesKilled
    //DamageTaken

    
    #endregion
    
    
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

public class PH_WaveManager : MonoBehaviour
{
    //needs to be given the spawn points of each room
    //instantiates/teleports in enemies at the spawn points
    
    public static event Action OnRoomCleared;


    private void OnRoomCompleted()
    {
        OnRoomCleared?.Invoke();
    }
}


#endregion