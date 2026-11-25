using System;
using System.Collections.Generic;
using _Project.Scripts.Dungeon;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the game state (Menu, Hub, Playing, Store, Dead, Win), loads scenes, and runs the flow of a run.
/// Lives in the Managers prefab, so it stays loaded across every scene.
/// Other systems listen to its events instead of calling into it, except for the few public methods
/// (LoadScene, RegisterPlayer, InitializeRun, ChangeRoom, RequestGameEnd, TryShowRewards, ChooseReward, SkipReward).
/// </summary>
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
        Win,     // The boss is defeated. The win screen is showing.
    }

    /// <summary>How a run ended. Passed to RequestGameEnd.</summary>
    public enum RunResult
    {
        Victory,
        Lose,
    }

    [Tooltip("Shown in the Inspector for debugging. Don't set it by hand.")]
    [SerializeField] private GameState _currentState;
    [Tooltip("The registered player's Health. Set by RegisterPlayer.")]
    [SerializeField] private Health _playerHealth;
    [Tooltip("The registered player's root object. Set by RegisterPlayer.")]
    [SerializeField] private GameObject _player;
    [Tooltip("The Level scene's WaveManager. Found when the Level loads.")]
    [SerializeField] private WaveManager _waveManager;
    [Tooltip("Rolls the gun parts offered when a combat room is cleared.")]
    [SerializeField] private ItemRolling _rewardPool;

    [Header("Hitstop")]
    [Tooltip("Seconds of frozen time when the player's bullet kills an enemy. 0 turns it off.")]
    [SerializeField] private float _killHitstopDuration = 0.05f;
    [Tooltip("Seconds of frozen time when the player's bullet kills the boss.")]
    [SerializeField] private float _bossHitstopDuration = 0.2f;

    private GameState _previousState;
    private Room _rewardRoom;
    private IReadOnlyList<WeightedReward<GunPart>> _pendingRewards;
    private Action _afterRewards;
    private bool _hasSavedProgress;
    private bool _isPaused;
    private bool _isLoading;
    private GameState _stateAfterLoad;
    private PlayerSpawner _playerSpawner;
    private LevelGenerator _generator;
    private Gun _playerGun;
    private bool _isHitstopActive;
    private float _hitstopEndTime;

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
    /// <summary>Fires when a scene's player registers, with its PlayerRoot.</summary>
    public event Action<PlayerRoot> OnPlayerRegistered;
    public event Action<Room> OnRoomEntered;

    /// <summary>Time.time when the current run started.</summary>
    public float StartTime { get; private set; }
    /// <summary>How long the last run took, in seconds. Set when the run ends.</summary>
    public float EndTime { get; private set; }
    /// <summary>Seconds since the current run started.</summary>
    public float RunTime => Time.time - StartTime;

    /// <summary>The current or last run's damage, kills, and crystals. The death and win screens read it.</summary>
    public RunStats RunStats { get; } = new RunStats();

    /// <summary>The room the player is in. Room.EnterRoom sets it.</summary>
    public Room CurrentRoom { get; private set; }
    public GameState CurrentState => _currentState;
    /// <summary>The registered player's root object, the one with PlayerRoot.</summary>
    public GameObject Player => _player;

    /// <summary>Progress kept between runs and scenes for this play session.</summary>
    public PersistentProgress Progress { get; private set; } = new PersistentProgress();

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        SceneManager.sceneLoaded += InitReferences;
        UIManager.OnPauseRequested += SetPause;
        UIManager.OnResetRequested += ResetGame;
        UIManager.OnStartRequested += StartHub;
        UIManager.OnMenuRequested += MainMenu;
        UIManager.OnExitRequested += ExitGame;
        UIManager.OnStoreRequested += ViewStore;
        Room.OnRoomCleared += RoomCleared;
    }

    private void Start()
    {
        if (Instance != this) return;

        // Pressing Play in the Hub or Level scene skips the menu, so start in that scene's state.
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (sceneIndex == Scenes.MainMenu)
        {
            ChangeState(GameState.Menu, force: true);
        }
        else if (sceneIndex == Scenes.Hub)
        {
            ChangeState(GameState.Hub, force: true);
        }
        else
        {
            ChangeState(GameState.Playing, force: true);
        }

        // Subscribed here, because InputManager may not have run its Awake yet when this Awake runs.
        InputManager.Instance.OnPausePressed += HandleEscapePressed;
    }

    private void OnEnable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnDied += PlayerDied;
        }
        if (_playerGun != null)
        {
            _playerGun.OnDamageDealt += HandleDamageDealt;
        }
    }

    // HasInstance guards against InputManager quitting first.
    private void OnDisable()
    {
        if (InputManager.HasInstance)
        {
            InputManager.Instance.OnPausePressed -= HandleEscapePressed;
        }
        if (_playerHealth != null)
        {
            _playerHealth.OnDied -= PlayerDied;
        }
        if (_playerGun != null)
        {
            _playerGun.OnDamageDealt -= HandleDamageDealt;
        }
        if (_playerSpawner != null)
        {
            _playerSpawner.OnPlayerSpawned -= StartRun;
        }
        SceneManager.sceneLoaded -= InitReferences;
        Room.OnRoomCleared -= RoomCleared;
        RunStats.Stop();
    }

    private void Update()
    {
        if (_isHitstopActive && Time.unscaledTime >= _hitstopEndTime)
        {
            EndHitstop();
        }
    }

    /// <summary>
    /// Runs after every scene load. In the Level scene, it only stores the generator and WaveManager, because the run
    /// starts later from StartRun. In the Hub and the menu, it enters the state LoadScene asked for.
    /// </summary>
    private void InitReferences(Scene scene, LoadSceneMode mode)
    {
        _isLoading = false;

        if (scene.buildIndex == Scenes.Level)
        {
            _generator = LevelGenerator.Instance;
            _waveManager = FindFirstObjectByType<WaveManager>();
            return;
        }

        // UIManager.Instance.ShowLoading(false);   // Turn on when the loading screen exists.
        ChangeState(_stateAfterLoad, force: true);
    }

    /// <summary>
    /// Called by PlayerRoot.Start in each scene. Stores the player, moves the death listener to the new Health,
    /// loads saved progress, and raises OnPlayerRegistered. In the Level, it also starts generation.
    /// </summary>
    /// <param name="newPlayer">The PlayerRoot of the player that just started.</param>
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

        Gun newGun = newPlayer.Gun;
        if (newGun != _playerGun)
        {
            if (_playerGun != null)
            {
                _playerGun.OnDamageDealt -= HandleDamageDealt;
            }
            _playerGun = newGun;
            if (_playerGun != null)
            {
                _playerGun.OnDamageDealt += HandleDamageDealt;
            }
        }

        // Loads the crystals and charms saved by the last LoadScene. Skipped until the first save.
        if (_hasSavedProgress && newPlayer.Inventory != null)
        {
            Progress.LoadInto(newPlayer.Inventory);
        }
        OnPlayerRegistered?.Invoke(newPlayer);

        if (SceneManager.GetActiveScene().buildIndex == Scenes.Level)
        {
            // sceneLoaded can run after PlayerRoot.Start, so find the generator here if InitReferences hasn't yet.
            if (_generator == null) _generator = LevelGenerator.Instance;

            // PlayerSpawner places the player in the start room once the map is built and the NavMesh is baked,
            // then fires OnPlayerSpawned, which starts the run.
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
        OnRoomEntered?.Invoke(newRoom);
    }

    /// <summary>
    /// Ends the run with a result. Any system can ask, and the request is ignored outside a run.
    /// </summary>
    /// <param name="result">Victory enters Win, and Lose enters Dead.</param>
    public void RequestGameEnd(RunResult result)
    {
        if (_currentState != GameState.Playing && _currentState != GameState.Store) return;

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
    public void LoadScene(int buildIndex, GameState stateAfterLoad)
    {
        if (_isLoading) return;
        _isLoading = true;
        _stateAfterLoad = stateAfterLoad;

        SaveProgress();
        RunStats.Stop(); // Leaving mid-run from the pause menu never reaches EnterDead or EnterWin.

        // Clear the pause without running SetPause, which would turn the old player's input back on.
        _isPaused = false;
        _isHitstopActive = false;
        Time.timeScale = 1f;
        OnPauseToggled?.Invoke(false);

        InputManager.Instance.SetMode(InputMode.Disabled); // Each state sets its own mode again after the load.
        // UIManager.Instance.ShowLoading(true);   // Turn on when the loading screen exists.

        SceneManager.LoadSceneAsync(buildIndex);
    }

    #region Rewards

    /// <summary>
    /// Equips the reward at the index on the player's gun, marks it obtained, and runs the portal's teleport.
    /// Returns false if the part couldn't be equipped. RewardScreen then offers SkipReward.
    /// </summary>
    public bool ChooseReward(int index)
    {
        if (_rewardPool == null || index < 0 || index >= _rewardPool.RewardOptions.Count) return false;

        GunPart part = _rewardPool.RewardOptions[index].Item;
        Gun gun = GetPlayerGun();

        if (part == null || gun == null) return false;

        if (!gun.HasSocket(part.PartType))
        {
            Debug.LogError($"Gun has no socket for {part.PartType}", gun);
            return false;
        }

        if (!gun.EquipPart(part))
        {
            Debug.LogError($"EquipPart failed for {part.PartName}", gun);
            return false;
        }

        _rewardPool.PickReward(index);
        FinishRewards();
        return true;
    }

    /// <summary>Closes the reward without a part, then runs the portal's teleport. RewardScreen calls this.</summary>
    public void SkipReward()
    {
        FinishRewards();
    }

    /// <summary>
    /// Opens the reward screen if the room has rewards waiting. Portal calls this before teleporting, and passes the
    /// teleport as afterRewards, which runs once the player picks or skips.
    /// </summary>
    /// <returns>True if the screen opened. False means there's nothing to show, so the portal teleports right away.</returns>
    public bool TryShowRewards(Room room, Action afterRewards)
    {
        if (room == null || room != _rewardRoom || _pendingRewards == null) return false;

        IReadOnlyList<WeightedReward<GunPart>> rewards = _pendingRewards;
        _pendingRewards = null;
        _afterRewards = afterRewards;

        UIManager.Instance.HandleRewards(rewards);
        return true;
    }

    /// <summary>Clears the waiting rewards and runs the portal's teleport, if there is one.</summary>
    private void FinishRewards()
    {
        Action afterRewards = _afterRewards;
        ClearPendingRewards();
        afterRewards?.Invoke();
    }

    private void ClearPendingRewards()
    {
        _rewardRoom = null;
        _pendingRewards = null;
        _afterRewards = null;
    }

    private Gun GetPlayerGun()
    {
        return _player != null ? _player.GetComponentInChildren<Gun>() : null;
    }

    #endregion

    #region Event Handlers

    private void ViewStore()
    {
        ChangeState(GameState.Store);
    }

    /// <summary>Quits the game. Raised by the menu's Quit button through UIManager.</summary>
    private void ExitGame()
    {
        Application.Quit();
    }

    /// <summary>Closes the store if it's open, ignores the press on the reward screen, and toggles pause otherwise.</summary>
    private void HandleEscapePressed()
    {
        if (_currentState == GameState.Store)
        {
            UIManager.Instance.CloseScreen();
            return;
        }

        if (UIManager.Instance.IsRewardOpen) return;

        SetPause(!_isPaused);
    }

    private void PlayerDied(GameObject player)
    {
        RequestGameEnd(RunResult.Lose);
    }

    /// <summary>Loads the Hub. Raised by the menu's Start button through UIManager.</summary>
    private void StartHub()
    {
        LoadScene(Scenes.Hub, GameState.Hub);
    }

    /// <summary>Loads the main menu. Raised by the pause screen's Menu button through UIManager.</summary>
    private void MainMenu()
    {
        LoadScene(Scenes.MainMenu, GameState.Menu);
    }

    /// <summary>
    /// Starts the run once PlayerSpawner has placed the player in the start room. Resets the reward pool, stores the
    /// dungeon, enters the start room, switches to Playing, and starts RunStats and the run timer.
    /// </summary>
    private void StartRun()
    {
        _playerSpawner.OnPlayerSpawned -= StartRun;
        _rewardPool.ResetObtained();
        ClearPendingRewards();

        // EnterRoom calls ChangeRoom and fires the room's OnRoomEntered.
        Room startRoom = _generator.StartRoom;
        startRoom.EnterRoom();

        // UIManager.Instance.ShowLoading(false);   // Turn on when the loading screen exists.
        ChangeState(GameState.Playing);
        RunStats.Begin(_player.GetComponent<PlayerRoot>(), _waveManager);
        StartTime = Time.time;
        OnRunStarted?.Invoke();
    }

    /// <summary>
    /// Rolls the cleared room's rewards. The rewards wait until the player walks into the room's portal (see
    /// TryShowRewards). A room whose portal leads nowhere, such as the End room, shows them right away. The room rolls
    /// its own chest in Room.OnComplete.
    /// </summary>
    private void RoomCleared(Room room)
    {
        if (room == null || _rewardPool == null) return;

        IReadOnlyList<WeightedReward<GunPart>> rewards = _rewardPool.RollRewards(GetPlayerGun());
        if (rewards.Count == 0) return;

        _rewardRoom = room;

        if (room.Portal == null || room.Portal.Connected == null)
        {
            UIManager.Instance.HandleRewards(rewards);
            return;
        }

        _pendingRewards = rewards;
    }

    /// <summary>Starts a run by loading the Level scene. RunStartPortal calls this from the Hub.</summary>
    public void InitializeRun()
    {
        LoadScene(Scenes.Level, GameState.Playing);
    }

    #endregion

    #region State Management

    /// <summary>
    /// Switches to a new state and runs its Enter method. Ignores a switch to the current state
    /// unless forced, and unpauses first.
    /// </summary>
    /// <param name="newState">The state to switch to.</param>
    /// <param name="force">True runs the Enter method even if the state is already newState.</param>
    [Button, DisableInEditorMode]
    private void ChangeState(GameState newState, bool force = false)
    {
        if (_currentState == newState && !force) return;

        if (_isPaused)
        {
            SetPause(false); // Unpause when leaving to the menu or the Hub.
        }

        bool returningFromStore = _currentState == GameState.Store && newState == GameState.Hub;

        // Only a real change is saved. Entering Store from Store would save Store, and CloseState would stay there.
        if (newState != _currentState)
        {
            _previousState = _currentState;
        }
        _currentState = newState;

        switch (newState)
        {
            case GameState.Menu:
                EnterMenu();
                SoundController.Instance.PlayMusic("MenuMusic");
                break;
            case GameState.Hub:
                EnterHub();
                if (!returningFromStore)
                {
                    SoundController.Instance.PlayMusic(UnityEngine.Random.value < 0.5f ? "Hub1" : "Hub2");
                }
                break;
            case GameState.Playing:
                EnterPlaying();
                SoundController.Instance.PlayMusic("FightTheme");
                break;
            case GameState.Dead:
                EnterDead();
                SoundController.Instance.PlayMusic("FightTheme");
                break;
            case GameState.Win:
                EnterWin();
                SoundController.Instance.PlayMusic("FightTheme");
                break;
            case GameState.Store:
                EnterStore();
                break;
        }

        OnGameStateChanged?.Invoke(_currentState);
    }

    // Each Enter method sets the input mode. Gameplay mode turns the Player action map on, and UI mode turns the UI map on.

    private void EnterPlaying()
    {
        InputManager.Instance.SetMode(InputMode.Gameplay);
    }

    private void EnterMenu()
    {
        InputManager.Instance.SetMode(InputMode.UI);
    }

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
    private void SetPause(bool pauseIntended)
    {
        if (_currentState != GameState.Playing && _currentState != GameState.Hub) return;
        if (_player == null) return; // No player registered yet, so there's nothing to freeze.

        bool pauseChanged = _isPaused != pauseIntended;
        _isPaused = pauseIntended;
        _isHitstopActive = false;

        if (pauseIntended)
        {
            InputManager.Instance.SetMode(InputMode.UI);
            Time.timeScale = 0;
        }
        else
        {
            InputManager.Instance.SetMode(InputMode.Gameplay);
            Time.timeScale = 1;
        }

        if (pauseChanged)
        {
            OnPauseToggled?.Invoke(_isPaused);
        }
    }

    /// <summary>Leaves the Store and returns to the state before it.</summary>
    public void CloseState()
    {
        if (_currentState != GameState.Store) return;

        ChangeState(_previousState);
    }

    /// <summary>Returns to the Hub. Raised by the death, win, and pause screens' Hub buttons through UIManager.</summary>
    private void ResetGame()
    {
        LoadScene(Scenes.Hub, GameState.Hub);
    }

    /// <summary>Stores how long the run took and stops counting RunStats, for the death and win screens.</summary>
    private void CaptureTime()
    {
        EndTime = Time.time - StartTime;
        RunStats.Stop();
    }

    #endregion

    #region Hitstop

    private void HandleDamageDealt(GameObject target, DamageInfo damageInfo)
    {
        if (_currentState != GameState.Playing && _currentState != GameState.Win) return;
        if (target == null || !target.TryGetComponent(out Health health) || health.CurrentHealth > 0) return;

        bool isBoss = target.TryGetComponent(out StoneGuardian _);
        if (_currentState == GameState.Win && !isBoss) return;

        StartHitstop(isBoss ? _bossHitstopDuration : _killHitstopDuration);
    }

    public void StartHitstop(float duration)
    {
        if (_isPaused || duration <= 0f) return;

        float endTime = Time.unscaledTime + duration;
        _hitstopEndTime = _isHitstopActive ? Mathf.Max(_hitstopEndTime, endTime) : endTime;
        _isHitstopActive = true;
        Time.timeScale = 0f;
    }

    private void EndHitstop()
    {
        _isHitstopActive = false;

        if (!_isPaused)
        {
            Time.timeScale = 1f;
        }
    }

    #endregion

    #region Debug

    // Select the Managers object in Play mode to use these buttons.

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
        InitializeRun();
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