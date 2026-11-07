using System;
using System.Collections;
using System.Linq.Expressions;
using _Project.Scripts.Dungeon;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


public class GameManager : MGR<GameManager>
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
    [SerializeField] private Health _playerHealthbar;
    [SerializeField] private GameObject player;
    [SerializeField] private PH_WaveManager waveManager; //wave manager responsible for spawning in enemies?
    [SerializeField] private Room[,] currentDungeon;
    
    
    private bool _isPaused;
    private float _runTime;
    //should rooms carry their own timer? i think so 
    LevelGenerator generator;
    
    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }
        SceneManager.sceneLoaded += InitReferences; //init references after scene load
        UIManager.OnPauseRequested += TogglePause;
        UIManager.OnResetRequested += ResetGame;
        UIManager.OnStartRequested += StartHub;
        UIManager.OnMenuRequested += MainMenu;
        UIManager.OnExitRequested += ExitGame;
        //LevelGenerator.Instance.OnMapGenerated += StartRun;
        PH_Boss.OnBossDefeated += BossDefeated;
        PH_WaveManager.OnRoomCleared += RoomCleared;
    }

    private void Start() 
    {
      
        ChangeState(GameState.Menu, force: true); 
    }

    private void OnEnable()
    {
        if(_playerHealthbar != null)
        {
            _playerHealthbar.OnDied += PlayerDied;
        }
    }

    private void InitReferences(Scene scene, LoadSceneMode mode)
    {
        if(scene.buildIndex != Scenes.MainMenu) //Scenes struct is on line 370 to avoid magic numbers
        {
            if (player == null)
            {
                player = GameObject.Find("Player"); //TODO: move this to a load function
            }

            if (player != null)
            {
                Health newHealth = player.GetComponentInChildren<Health>();
                if (newHealth != _playerHealthbar)
                {
                    if (_playerHealthbar != null)
                    {
                        _playerHealthbar.OnDied -= PlayerDied;
                    }
                    _playerHealthbar = newHealth;
                    if (_playerHealthbar != null)
                    {
                        _playerHealthbar.OnDied += PlayerDied;
                    }
                }
            }

            if (scene.buildIndex == Scenes.Dungeon)
            {
                generator = LevelGenerator.Instance;
            }
           
        }
    }


    private void Update()
    {

        if (Keyboard.current.escapeKey.wasPressedThisFrame) //maybe put this in a bigger control thing? 
        {
            TogglePause();
        }
        
        #if UNITY_EDITOR

        if (Keyboard.current.slashKey.wasPressedThisFrame) //DEBUG FOR TESTING PLAYING STATE REQUIREMENTS
        {
            ChangeState(GameState.Playing);
        }
        
        #endif

        DEBUGInitializeRun();
    }

    private void DEBUGInitializeRun()
    {
        if (currentState == GameState.Hub && Keyboard.current.kKey.wasPressedThisFrame) //updated to use new input system
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

    private void ExitGame()
    {
        Application.Quit();
    }
    private void PlayerDied(GameObject player)  
    {
        RequestGameEnd(RunResult.Lose);
    }
    
    private void StartHub()
    {
        ChangeState(GameState.Hub);
    }

    private void MainMenu()
    {
        ChangeState(GameState.Menu);
    }
    
    /// <summary>
    /// happens after the generation request to the level gen is completed,
    /// fills in necessary variables for tracking the dungeon here
    /// and handles the first swap to playing state for that dungeon (otherwise bugs between playing states)
    /// </summary>
    /// <param name="generator">The level generator object that contains the dungeon</param>
    private void StartRun() 
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
    /// <param name="force">Whether to force the state transition.</param>
    private void ChangeState(GameState newState, bool force = false)
    {
        if (currentState == newState && !force)
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

            player.GetComponentInChildren<PlayerMovement>().enabled = true;
            player.GetComponentInChildren<PlayerInput>().enabled = true;
            player.GetComponentInChildren<Shoot>().enabled = true;
            player.GetComponentInChildren<Dash>().enabled = true;
            
        }
        else
        {
            Time.timeScale = 0;
            _isPaused = true;

                player.GetComponentInChildren<PlayerMovement>().enabled = false;
                player.GetComponentInChildren<PlayerInput>().enabled = false;
                player.GetComponentInChildren<Shoot>().enabled = false;
                player.GetComponentInChildren<Dash>().enabled = false;

                

        }
        OnPauseToggle?.Invoke(_isPaused);
    }
    
    private void ResetGame() //used to have a call that prevents unless state was win or loss, didnt work with pause or main menu so uhh removed it, hope that wasnt for any reason :^ )
    {
        ChangeState(GameState.Hub); 
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

    private void OnDisable()
    {
        _playerHealthbar.OnDied -= PlayerDied;
        SceneManager.sceneLoaded -= InitReferences;
    }



}

public struct Scenes
{
    public const int MainMenu = 0;
    public const int Hub = 1;
    public const int Dungeon = 2;
}


#region PLACEHOLDERS


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