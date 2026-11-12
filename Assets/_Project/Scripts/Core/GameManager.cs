using System;
using _Project.Scripts.Dungeon;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

/// <summary>
/// Owns the game state (Menu, Hub, Playing, Store, Dead, Win), loads scenes, and runs the flow of a run.
/// Lives in the Managers prefab, so it stays loaded across every scene.
/// Other systems listen to its events instead of calling into it, except for the few public methods
/// (LoadScene, RegisterPlayer, InitializeRun, ChangeRoom, RequestGameEnd).
/// </summary>
// INTEGRATION: was a MonoBehaviour with its own Instance and DontDestroyOnLoad. Now inherits MGR<GameManager>,
// and ManagerBootstrap creates it from the Managers prefab before the first scene loads.
public class GameManager : MGR<GameManager>
{
    /// <summary>The states the whole game can be in. Pausing is a flag inside Hub and Playing, not a state.</summary>
    public enum GameState
    {
        Menu,    // Main menu on startup.
        Hub,     // The area between runs.
        Playing, // A run in the Dungeon.
        Store,   // The shop or charm screen is open in the Hub.
        Dead,    // The player died. The death screen is showing.
        Win      // The boss is defeated. The win screen is showing.
    }

    /// <summary>How a run ended. Passed to RequestGameEnd.</summary>
    public enum RunResult
    {
        Victory,
        Lose,
    }

    /// <summary>Fires when the run ends in a loss. UIManager shows the death screen.</summary>
    public static event Action OnGameLost;
    /// <summary>Fires when the run ends in a victory. UIManager shows the win screen.</summary>
    public static event Action OnGameWon;
    /// <summary>Fires when the Level player registers. LevelGenerator builds the dungeon when it hears this.</summary>
    public static event Action OnGenerateRequested;
    /// <summary>Fires after the player is placed in the start room and the state is Playing.</summary>
    public static event Action OnRunStarted;
    /// <summary>Fires when pause turns on or off. The value is true while paused.</summary>
    public static event Action<bool> OnPauseToggled;
    /// <summary>Fires after every state change, with the new state.</summary>
    public event Action<GameState> OnGameStateChanged;
    // INTEGRATION: new. The HUD bars, CurrencyDisplay, ConsumableSlotsUI, and InteractPrompt connect to each new player through this,
    // because the Hub and the Dungeon each create their own player.
    /// <summary>Fires when a scene's player registers, with its PlayerRoot.</summary>
    public event Action<PlayerRoot> OnPlayerRegistered;
    

    /// <summary>Time.time when the current run started.</summary>
    public float StartTime { get; private set; }
    /// <summary>How long the last run took, in seconds. Set when the run ends.</summary>
    public float EndTime { get; private set; }
    /// <summary>Seconds since the current run started.</summary>
    public float RunTime => Time.time - StartTime;

    /// <summary>The room the player is in. Room.EnterRoom sets it.</summary>
    public Room CurrentRoom { get; private set; }
    public GameState CurrentState => _currentState; // INTEGRATION: new, so other scripts can read the state without the event.
    // INTEGRATION: new. Enemies and debug buttons find the player through this instead of GameObject.Find("Player").
    /// <summary>The registered player's root object, the one with PlayerRoot.</summary>
    public GameObject Player => _player;

    [SerializeField] private GameState currentState; //serialized for viewing in editor
    [SerializeField] private Health _playerHealthbar;
    [SerializeField] private GameObject player;
    [SerializeField] private Room[,] currentDungeon;
    [SerializeField] private RoomCompletionLootPool _lootPool;
    // INTEGRATION: new. Carries crystals and charms between scenes. LoadScene saves into it and RegisterPlayer loads from it.
    /// <summary>Progress kept between runs and scenes for this play session.</summary>
    public PersistentProgress Progress { get; private set; } = new PersistentProgress();

    [Tooltip("Shown in the Inspector for debugging. Don't set it by hand.")]
    [SerializeField, FormerlySerializedAs("currentState")] private GameState _currentState;
    [Tooltip("The registered player's Health. Set by RegisterPlayer.")]
    [SerializeField, FormerlySerializedAs("_playerHealthbar")] private Health _playerHealth;
    [Tooltip("The registered player's root object. Set by RegisterPlayer.")]
    [SerializeField, FormerlySerializedAs("player")] private GameObject _player; // INTEGRATION: set by RegisterPlayer instead of GameObject.Find.
    [Tooltip("Placeholder until the wave manager is built in Sprint 3.")]
    [SerializeField, FormerlySerializedAs("waveManager")] private PH_WaveManager _waveManager;

    private Room[,] _currentDungeon;   // The generated room grid, copied from LevelGenerator when the run starts.
    private bool _hasSavedProgress;    // INTEGRATION: new. False until the first save, so the first player keeps the values set on its prefab.
    private bool _isPaused;            // True while the game is paused. Pausing is a flag, not a GameState.
    private bool _isLoading;           // INTEGRATION: new. Blocks a second LoadScene call while a scene is loading.
    private GameState _stateAfterLoad; // INTEGRATION: new. The state LoadScene was asked to enter once the scene finishes loading.
    private PlayerSpawner _playerSpawner; // INTEGRATION: new. The Level player's spawner. StartRun listens to its OnPlayerSpawned.
    private LevelGenerator _generator; // The Level scene's generator. Set in InitReferences or RegisterPlayer.

    /// <summary>Subscribes to scene loads and to the UI's request events.</summary>
    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }

        // INTEGRATION: pressing Play in the Hub or Level scene skips the menu, so start in Playing there.
        if (SceneManager.GetActiveScene().buildIndex != Scenes.MainMenu)
        {
            ChangeState(GameState.Playing, force: true);
        }

        SceneManager.sceneLoaded += InitReferences; // INTEGRATION: new. Finishes each scene change after the scene loads.
        UIManager.OnPauseRequested += TogglePause;
        UIManager.OnResetRequested += ResetGame;
        UIManager.OnStartRequested += StartHub;
        UIManager.OnMenuRequested += MainMenu;
        UIManager.OnExitRequested += ExitGame;
        //LevelGenerator.Instance.OnMapGenerated += StartRun;
        Room.OnRoomCleared += RoomCleared;
        PH_Boss.OnBossDefeated += BossDefeated;
        
    }

    /// <summary>Enters the Menu state when the game starts in the menu, and listens to the pause key.</summary>
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

        // INTEGRATION: replaces reading the Escape key in Update. Subscribed in Start because InputManager
        // may not have run its Awake yet when this Awake runs.
        InputManager.Instance.OnPausePressed += TogglePause;
    }

    /// <summary>Listens to the player's death, if a player was assigned before this component turned on.</summary>
    private void OnEnable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnDied += PlayerDied;
        }
    }

    /// <summary>
    /// Runs after every scene load. In the Level scene, it only stores the generator, because the run starts
    /// later from StartRun. In the Hub and the menu, it enters the state LoadScene asked for.
    /// </summary>
    // INTEGRATION: new. Changing state after the load, never before, means the new scene's objects hear the change.
    private void InitReferences(Scene scene, LoadSceneMode mode)
    {
        _isLoading = false;

        if (scene.buildIndex == Scenes.Level)
        {
            _generator = LevelGenerator.Instance;
            return;
        }

        // UIManager.Instance.ShowLoading(false);   // Turn on when the loading screen exists.
        ChangeState(_stateAfterLoad);
    }

    /// <summary>
    /// Called by PlayerRoot.Start in each scene. Stores the player, moves the death listener to the new Health,
    /// loads saved progress, and raises OnPlayerRegistered. In the Level, it also starts generation.
    /// </summary>
    /// <param name="newPlayer">The PlayerRoot of the player that just started.</param>
    // INTEGRATION: new. Replaces GameObject.Find("Player") and finding Health in Start.
    public void RegisterPlayer(PlayerRoot newPlayer)
    {
        _player = newPlayer.gameObject;

        // Move the OnDied subscription from the old player's Health to the new one.
        Health newHealth = newPlayer.PlayerHealth;
        if (newHealth != _playerHealth)
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnDied -= PlayerDied;
            }
            _playerHealth = newHealth;
            if (_playerHealth != null)
            {
                _playerHealth.OnDied += PlayerDied;
            }
        }

        // INTEGRATION: loads the crystals and charms saved by the last LoadScene. Skipped until the first save.
        if (_hasSavedProgress && newPlayer.Inventory != null)
        {
            Progress.LoadInto(newPlayer.Inventory);
        }
        OnPlayerRegistered?.Invoke(newPlayer);

        if (SceneManager.GetActiveScene().buildIndex == Scenes.Level)
        {
            // INTEGRATION: sceneLoaded can run after PlayerRoot.Start, so find the generator here if InitReferences hasn't yet.
            if (_generator == null) _generator = LevelGenerator.Instance;

            // INTEGRATION: PlayerSpawner replaces the temporary generator.OnMapGenerated subscription. It places the player
            // in the start room once the map is built and the NavMesh is baked, then fires OnPlayerSpawned, which starts the run.
            if (_playerSpawner != null) _playerSpawner.OnPlayerSpawned -= StartRun;
            _playerSpawner = newPlayer.GetComponentInChildren<PlayerSpawner>();
            if (_playerSpawner == null)
            {
                Debug.LogError("GameManager: the player has no PlayerSpawner, so the run can't start.", newPlayer);
                return;
            }
            // Everything has to be listening before the request, because generation finishes in the same frame.
            _playerSpawner.OnPlayerSpawned += StartRun;
            _playerSpawner.SpawnWhenMapReady(_generator);
            OnGenerateRequested?.Invoke();
        }
    }

    /// <summary>Copies the current player's crystals and charms into Progress.</summary>
    // INTEGRATION: new. LoadScene calls it before the old scene and its player unload. PersistentProgress.SaveFrom does the copy.
    private void SaveProgress()
    {
        if (_player == null) return;

        PlayerRoot root = _player.GetComponent<PlayerRoot>();
        if (root == null || root.Inventory == null) return;

        Progress.SaveFrom(root.Inventory);
        _hasSavedProgress = true;
    }

    /// <summary>Stores the room the player is in. Room.EnterRoom calls this.</summary>
    /// <param name="newRoom">The room the player just entered.</param>
    public void ChangeRoom(Room newRoom)
    {
        CurrentRoom = newRoom;
    }

    /// <summary>
    /// Ends the run with a result. Any system can ask, and the request is ignored outside a run.
    /// </summary>
    /// <param name="result">Victory enters Win, and Lose enters Dead.</param>
    public void RequestGameEnd(RunResult result)
    {
        if (_currentState != GameState.Playing && _currentState != GameState.Store) // Only during a run.
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

    /// <summary>
    /// The only way scenes change. Saves progress, clears pause, turns input off, and loads the scene.
    /// InitReferences finishes the change once the scene has loaded.
    /// </summary>
    /// <param name="buildIndex">A value from <see cref="Scenes"/>.</param>
    /// <param name="stateAfterLoad">The state to enter after the scene loads. Ignored for the Level, where StartRun sets the state.</param>
    // INTEGRATION: new. Before this, state changes didn't load scenes at all.
    public void LoadScene(int buildIndex, GameState stateAfterLoad)
    {
        if (_isLoading) return;
        _isLoading = true;
        _stateAfterLoad = stateAfterLoad;

        SaveProgress(); // INTEGRATION: saves before the player is unloaded with the scene.

        // Clear the pause without running TogglePause, which would turn the old player's input back on.
        _isPaused = false;
        Time.timeScale = 1f;
        OnPauseToggled?.Invoke(false);

        InputManager.Instance.SetMode(InputMode.Disabled); // INTEGRATION: no input while loading. Each state sets its mode again.
        // UIManager.Instance.ShowLoading(true);   // Turn on when the loading screen exists.

        SceneManager.LoadSceneAsync(buildIndex);
    }

    #region EventHandlers

    /// <summary>Quits the game. Raised by the menu's Quit button through UIManager.</summary>
    private void ExitGame()
    {
        Application.Quit();
    }

    /// <summary>Ends the run as a loss when the player's Health fires OnDied.</summary>
    private void PlayerDied(GameObject player)
    {
        RequestGameEnd(RunResult.Lose);
    }

    /// <summary>Loads the Hub. Raised by the menu's Start button through UIManager.</summary>
    private void StartHub()
    {
        LoadScene(Scenes.Hub, GameState.Hub); // INTEGRATION: loads the Hub scene instead of only changing the state.
    }

    /// <summary>Loads the main menu. Raised by the pause screen's Menu button through UIManager.</summary>
    private void MainMenu()
    {
        LoadScene(Scenes.MainMenu, GameState.Menu); // INTEGRATION: loads the menu scene instead of only changing the state.
    }

    /// <summary>
    /// Starts the run once PlayerSpawner has placed the player in the start room. Stores the dungeon,
    /// enters the start room, switches to Playing, and starts the run timer.
    /// </summary>
    private void StartRun()
    {
        _playerSpawner.OnPlayerSpawned -= StartRun; 
        _lootPool.ResetObtained(); //make sure the data asset is cleared
        currentDungeon = generator.RoomGrid;
        Room startRoom = generator.StartRoom;
        startRoom.EnterRoom(); 
        _playerSpawner.OnPlayerSpawned -= StartRun; // INTEGRATION: listens to PlayerSpawner instead of LevelGenerator.OnMapGenerated.
        _currentDungeon = _generator.RoomGrid;
        Room startRoom = _generator.StartRoom;
        // INTEGRATION: was "CurrentRoom = startRoom". EnterRoom calls ChangeRoom and fires the room's OnRoomEntered,
        // which starts a RoomEncounter if the start room has enemies.
        startRoom.EnterRoom();

        // UIManager.Instance.ShowLoading(false);   // Turn on when the loading screen exists.
        ChangeState(GameState.Playing);
        StartTime = Time.time; // The run timer counts from here.
        OnRunStarted?.Invoke();
    }

    /// <summary>Ends the run as a victory. Placeholder: PH_Boss raises this until the boss exists.</summary>
    private void BossDefeated()
    {
        RequestGameEnd(RunResult.Victory);
    }

    private void RoomCleared(Room room)
    {
        Debug.Log("CHECK THIS CHECK THIS CHECK THIS CHECK THIS DONT FORGET");
        InputManager.Instance.SetMode(InputMode.UI);
    /// <summary>Placeholder for room clears. Empty: rooms clear through Room.OnRoomCleared now.</summary>
    private void RoomCleared()
    {
    }

    /// <summary>Starts a run by loading the Level scene. RunStartPortal calls this from the Hub.</summary>
    // INTEGRATION: made public, so RunStartPortal can call it.
    public void InitializeRun()
    {
        LoadScene(Scenes.Level, GameState.Playing);
    }

    #endregion

    #region StateManagement

    /// <summary>
    /// Switches to a new state and runs its Enter method. Ignores a switch to the current state
    /// unless forced, and unpauses first.
    /// </summary>
    /// <param name="newState">The state to switch to.</param>
    /// <param name="force">True runs the Enter method even if the state is already newState.</param>
    [Button, DisableInEditorMode]
    private void ChangeState(GameState newState, bool force = false) // INTEGRATION: force parameter added for startup.
    {
        if (_currentState == newState && !force)
        {
            return;
        }

        if (_isPaused)
        {
            TogglePause(); // Unpause when leaving to the menu or the Hub.
        }

        _currentState = newState;

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
        OnGameStateChanged?.Invoke(_currentState);
    }

    // INTEGRATION: each Enter method sets the input mode, which replaces turning player components on and off.
    // InputManager's Gameplay mode turns the Player action map on, and UI mode turns the UI map on.

    /// <summary>Gives the player control during a run.</summary>
    private void EnterPlaying()
    {
        InputManager.Instance.SetMode(InputMode.Gameplay);
    }

    /// <summary>Hands input to the menu UI.</summary>
    private void EnterMenu()
    {
        InputManager.Instance.SetMode(InputMode.UI);
    }

    /// <summary>Gives the player control in the Hub.</summary>
    private void EnterHub()
    {
        InputManager.Instance.SetMode(InputMode.Gameplay);
    }

    /// <summary>Records the run time, hands input to the UI, and raises OnGameLost.</summary>
    private void EnterDead()
    {
        CaptureTime();
        InputManager.Instance.SetMode(InputMode.UI);
        OnGameLost?.Invoke();
    }

    /// <summary>Records the run time, hands input to the UI, and raises OnGameWon.</summary>
    private void EnterWin()
    {
        CaptureTime();
        InputManager.Instance.SetMode(InputMode.UI);
        OnGameWon?.Invoke();
    }

    /// <summary>Hands input to the shop or charm screen.</summary>
    private void EnterStore()
    {
        InputManager.Instance.SetMode(InputMode.UI);
    }

    #endregion

    #region Misc

    /// <summary>
    /// Pauses or unpauses with the time scale and the input mode. Only works in the Hub and during a run,
    /// and only after a player has registered.
    /// </summary>
    [Button, DisableInEditorMode]
    private void TogglePause()
    {
        // INTEGRATION: pause also works in the Hub now.
        if (_currentState != GameState.Playing && _currentState != GameState.Hub)
        {
            return;
        }
        if (_player == null)
        {
            return; // No player registered yet, so there's nothing to freeze.
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
        OnPauseToggled?.Invoke(_isPaused);
    }

    /// <summary>Returns to the Hub. Raised by the death, win, and pause screens' Hub buttons through UIManager.</summary>
    private void ResetGame()
    {
        LoadScene(Scenes.Hub, GameState.Hub); // INTEGRATION: loads the Hub scene instead of only changing the state.
    }

    #endregion

    #region Stats

    /// <summary>Stores how long the run took, for the death and win screens.</summary>
    private void CaptureTime()
    {
        EndTime = Time.time - StartTime;
    }

    #endregion

    /// <summary>Removes every subscription this manager made. HasInstance guards against InputManager quitting first.</summary>
    private void OnDisable()
    {
        if (InputManager.HasInstance)
        {
            InputManager.Instance.OnPausePressed -= TogglePause;
        }
        if (_playerHealth != null)
        {
            _playerHealth.OnDied -= PlayerDied;
        }
        if (_playerSpawner != null)
        {
            _playerSpawner.OnPlayerSpawned -= StartRun;
        }
        SceneManager.sceneLoaded -= InitReferences;
    }

    #region Debug
    // INTEGRATION: the slash, K, and TimeDebug keys became these Inspector buttons. Select the Managers object in Play mode.

    /// <summary>Debug: enters Playing without starting a run.</summary>
    [Button, DisableInEditorMode]
    private void ForcePlaying()
    {
        ChangeState(GameState.Playing);
    }

    /// <summary>Debug: starts a run from anywhere, the same as the run-start portal.</summary>
    [Button, DisableInEditorMode]
    public void DEBUGInitializeRun()
    {
        LoadScene(Scenes.Level, GameState.Playing);
    }

    /// <summary>Debug: freezes time without pausing.</summary>
    [Button, DisableInEditorMode]
    private void PauseTime()
    {
        Time.timeScale = 0f;
    }

    /// <summary>Debug: slows time.</summary>
    /// <param name="scale">The time scale to use. 1 is normal speed.</param>
    [Button, DisableInEditorMode]
    private void SlowMotion(float scale = 0.2f)
    {
        Time.timeScale = scale;
    }

    /// <summary>Debug: returns time to normal speed.</summary>
    [Button, DisableInEditorMode]
    private void NormalSpeed()
    {
        Time.timeScale = 1f;
    }
    #endregion
}

/// <summary>Build indexes of the three scenes, in the order of the build settings.</summary>
// LoadScene takes these values.
public struct Scenes
{
    public const int MainMenu = 0;
    public const int Hub = 1;
    public const int Level = 2;
}
