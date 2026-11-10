using System;
using System.Collections;
using System.Linq.Expressions;
using _Project.Scripts.Dungeon;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;


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
    public event Action<PlayerRoot> OnPlayerRegistered;

    public float StartTime { get; private set; }
    public float EndTime { get; private set; }
    public float RunTime => Time.time - StartTime;

    public Room CurrentRoom { get; private set; }
    public GameState CurrentState => currentState;
    public GameObject Player => player;

    [SerializeField] private GameState currentState; //serialized for viewing in editor
    [SerializeField] private Health _playerHealthbar;
    [SerializeField] private GameObject player;
    [SerializeField] private PH_WaveManager waveManager; //wave manager responsible for spawning in enemies?
    [SerializeField] private Room[,] currentDungeon;


    
    public PersistentProgress Progress { get; private set; } = new PersistentProgress(); // CHANGED
    private bool _hasSavedProgress; 

    private bool _isPaused;
    private float _runTime;
    private bool _isLoading;
    private GameState _stateAfterLoad;
    private PlayerSpawner _playerSpawner; 
    //should rooms carry their own timer? i think so 
    LevelGenerator generator;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }

        if (SceneManager.GetActiveScene().buildIndex != Scenes.MainMenu)
        {
            ChangeState(GameState.Playing, force: true);

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
        if (Instance != this)
        {
            return;
        }
        if (SceneManager.GetActiveScene().buildIndex == Scenes.MainMenu)
        {
            ChangeState(GameState.Menu, force: true);
        }

        InputManager.Instance.PausePressed += TogglePause;
    }

    private void OnEnable()
    {
        if (_playerHealthbar != null)
        {
            _playerHealthbar.OnDied += PlayerDied;
        }

    }

    private void InitReferences(Scene scene, LoadSceneMode mode)
    {
        _isLoading = false;

        if (scene.buildIndex == Scenes.Level)
        {
            generator = LevelGenerator.Instance;
            return;
        }


        // UIManager.Instance.ShowLoading(false);               // Add in phase 5.
        ChangeState(_stateAfterLoad);
    }

    public void RegisterPlayer(PlayerRoot newPlayer)
    {
        player = newPlayer.gameObject;


        Health newHealth = newPlayer.PlayerHealth;
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

        if (_hasSavedProgress && newPlayer.Inventory != null) 
        {
            Progress.LoadInto(newPlayer.Inventory);        
        }
        OnPlayerRegistered?.Invoke(newPlayer);

        if (SceneManager.GetActiveScene().buildIndex == Scenes.Level)
        {
            if (generator == null) generator = LevelGenerator.Instance; 

          
            if (_playerSpawner != null) _playerSpawner.OnPlayerSpawned -= StartRun; 
            _playerSpawner = newPlayer.GetComponentInChildren<PlayerSpawner>();      
            if (_playerSpawner == null)                                             
            {
                Debug.LogError("GameManager: the player has no PlayerSpawner, so the run can't start.", newPlayer); 
                return;                                                             
            }
            _playerSpawner.OnPlayerSpawned += StartRun;            
            _playerSpawner.SpawnWhenMapReady(generator);           
            OnGenerateRequested?.Invoke();                         
        }
    }

    
    private void SaveProgress()
    {
        if (player == null) return;

        PlayerRoot root = player.GetComponent<PlayerRoot>();
        if (root == null || root.Inventory == null) return;

        Progress.SaveFrom(root.Inventory);
        _hasSavedProgress = true;
    }


    private void Update()
    {
        if (Instance != this)
        {
            return;
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

    public void LoadScene(int buildIndex, GameState stateAfterLoad)
    {
        if (_isLoading) return;
        _isLoading = true;
        _stateAfterLoad = stateAfterLoad;

        SaveProgress(); 

        
        _isPaused = false;
        Time.timeScale = 1f;
        OnPauseToggle?.Invoke(false);

        InputManager.Instance.SetMode(InputMode.Disabled);
        // UIManager.Instance.ShowLoading(true);                // Add in phase 5.

        SceneManager.LoadSceneAsync(buildIndex);
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
        LoadScene(Scenes.Hub, GameState.Hub);
    }

    private void MainMenu()
    {
        LoadScene(Scenes.MainMenu, GameState.Menu);
    }

    /// <summary>
    /// happens after the generation request to the level gen is completed,
    /// fills in necessary variables for tracking the dungeon here
    /// and handles the first swap to playing state for that dungeon (otherwise bugs between playing states)
    /// </summary>
    /// <param name="generator">The level generator object that contains the dungeon</param>
    private void StartRun()
    {
        _playerSpawner.OnPlayerSpawned -= StartRun; 
        currentDungeon = generator.RoomGrid;
        Room startRoom = generator.StartRoom;
        startRoom.EnterRoom(); 

        // UIManager.Instance.ShowLoading(false); 
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
    public void InitializeRun() 
    {
        LoadScene(Scenes.Level, GameState.Playing);
    }

    #endregion



    #region StateManagement
    [Button, DisableInEditorMode]
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
        InputManager.Instance.SetMode(InputMode.Gameplay);
    }
    private void EnterMenu()
    {
        //probably some code here about spawning back in the menu and despawning everything
        InputManager.Instance.SetMode(InputMode.UI);
    }

    private void EnterHub()
    {
        InputManager.Instance.SetMode(InputMode.Gameplay);
    }

    private void EnterDead()
    {
        CaptureTime();
        InputManager.Instance.SetMode(InputMode.UI);
        OnGameOver?.Invoke();
    }

    private void EnterWin()
    {
        CaptureTime();
        InputManager.Instance.SetMode(InputMode.UI);
        OnGameWin?.Invoke();
    }

    private void EnterStore()
    {
        InputManager.Instance.SetMode(InputMode.UI);
    }

    #endregion

    #region Misc

    [Button, DisableInEditorMode]
    private void TogglePause()
    {

        if (currentState != GameState.Playing && currentState != GameState.Hub)
        {
            return;
        }
        if (player == null)
        {
            return;   // No player registered yet, so there's nothing to freeze.
        }
        if (_isPaused)
        {
            InputManager.Instance.SetMode(InputMode.Gameplay);
            Time.timeScale = 1;
            _isPaused = false;
        }
        else
        {
            InputManager.Instance.SetMode(InputMode.UI);
            Time.timeScale = 0;
            _isPaused = true;
        }
        OnPauseToggle?.Invoke(_isPaused);
    }

    private void ResetGame() //used to have a call that prevents unless state was win or loss, didnt work with pause or main menu so uhh removed it, hope that wasnt for any reason :^ )
    {
        LoadScene(Scenes.Hub, GameState.Hub);
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
        if (InputManager.HasInstance)
        {
            InputManager.Instance.PausePressed -= TogglePause;
        }
        if (_playerHealthbar != null)
        {
            _playerHealthbar.OnDied -= PlayerDied;
        }
        if (_playerSpawner != null)                     
        {
            _playerSpawner.OnPlayerSpawned -= StartRun; 
        }
     
        SceneManager.sceneLoaded -= InitReferences;
    }

    #region Debug
    [Button, DisableInEditorMode]
    private void ForcePlaying()
    {
        ChangeState(GameState.Playing);
    }

    // Replaces K. Add the attributes to the method you already have.
    [Button, DisableInEditorMode]
    public void DEBUGInitializeRun()
    {
        LoadScene(Scenes.Level, GameState.Playing);
    }


    [Button, DisableInEditorMode]
    private void PauseTime()
    {
        Time.timeScale = 0f;
    }

    [Button, DisableInEditorMode]
    private void SlowMotion(float scale = 0.2f)
    {
        Time.timeScale = scale;
    }

    [Button, DisableInEditorMode]
    private void NormalSpeed()
    {
        Time.timeScale = 1f;
    }
    #endregion

}

public struct Scenes
{
    public const int MainMenu = 0;
    public const int Hub = 1;
    public const int Level = 2;
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