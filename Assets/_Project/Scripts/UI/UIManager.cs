using System;
using System.Collections.Generic;
using _Project.Scripts.Dungeon;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using _Project.Scripts.Dungeon;


using UnityEngine.Serialization;

/// <summary>
/// Shows and hides the UI screens, and sets the cursor for each. Screen buttons call the ButtonPressed methods, which
/// raise static request events that GameManager handles. Listens to GameManager for game over, win, pause, and state
/// changes.
/// </summary>
// INTEGRATION: now an MGR singleton (was a MonoBehaviour with a public static instance), so it persists between scenes
// with the other managers and is reached through UIManager.Instance.
public class UIManager : MGR<UIManager>
{
    /// <summary>Which group of screens is showing.</summary>
    private enum UIStates
    {
        StartMenu,
        PauseMenu,
        Gameplay,
        RewardChoice
    }

    [SerializeField, FormerlySerializedAs("screenList")] private List<UIScreen> _screenList; // Filled from children in Awake.
    [SerializeField, FormerlySerializedAs("menuScreen")] private MenuScreen _menuScreen;
    [SerializeField, FormerlySerializedAs("rewardScreen")] private RewardScreen _rewardScreen;
    [SerializeField, FormerlySerializedAs("settingsScreen")] private SettingsScreen _settingsScreen;
    [SerializeField, FormerlySerializedAs("deathScreen")] private DeathScreen _deathScreen;
    [SerializeField, FormerlySerializedAs("winScreen")] private WinScreen _winScreen;
    [SerializeField, FormerlySerializedAs("pauseScreen")] private PauseScreen _pauseScreen;
    [SerializeField, FormerlySerializedAs("currentState")] private UIStates _currentState;

    // Requests from the screen buttons. GameManager subscribes to each.
    /// <summary>Resume was pressed. GameManager toggles the pause.</summary>
    public static event Action OnPauseRequested;
    /// <summary>Start was pressed on the main menu. GameManager loads the Hub.</summary>
    public static event Action OnStartRequested;
    /// <summary>Restart was pressed. GameManager returns to the Hub.</summary>
    public static event Action OnResetRequested;
    /// <summary>Main Menu was pressed. GameManager loads the main menu.</summary>
    public static event Action OnMenuRequested;
    /// <summary>Quit was pressed. GameManager closes the game.</summary>
    public static event Action OnExitRequested;

    private UIScreen _activeScreen; // Not set yet.
    private bool _cursorVisible;    // The last value ToggleCursor set.

    /// <summary>Finds every screen, hides them, and starts listening to GameManager.</summary>
    protected override void Awake()
    {
        // INTEGRATION: MGR.Awake sets Instance and destroys duplicates, so a second UIManager from the next scene stops here.
        base.Awake();
        if (Instance != this)
        {
            return;
        }
        _screenList = new List<UIScreen>(GetComponentsInChildren<UIScreen>(true));
        HideAllScreens();

        // INTEGRATION: new. Starting Play mode in any scene other than the main menu goes straight to gameplay, which
        // locks the cursor.
        if (SceneManager.GetActiveScene().buildIndex != Scenes.MainMenu)
        {
            ChangeState(UIStates.Gameplay);
            HideAllScreens();
        }

        GameManager.OnGameLost += GameOver;
        GameManager.OnGameWon += GameWin;
        GameManager.OnPauseToggled += HandlePause;
        GameManager.Instance.OnGameStateChanged += HandleStateChanged;
        Room.OnRoomCleared += HandleRewards;

    }

    /// <summary>Debug: switches to a UI state.</summary>
    // INTEGRATION: the G debug key became this Inspector button.
    [Button, DisableInEditorMode]
    private void DEBUGChangeUIState(UIStates state)
    {
        ChangeState(state);
    }

    /// <summary>Hides every screen except the HUD and shows the ones for the new state.</summary>
    private void ChangeState(UIStates state)
    {
        _currentState = state;
        switch (state)
        {
            case UIStates.StartMenu:
                HideAllScreens();
                ShowScreen(_menuScreen);
                break;
            case UIStates.PauseMenu:
                HideAllScreens();
                ShowScreen(_pauseScreen);
                break;
            case UIStates.Gameplay:
                HideAllScreens();
                ToggleCursor(false);
                break;
            case UIStates.RewardChoice:
                HideAllScreens();
                ShowScreen(rewardScreen);
                break;
        }
    }

    #region ButtonHandlers

    /// <summary>Returns to gameplay and asks GameManager to unpause.</summary>
    public void ResumeButtonPressed()
    {
        ChangeState(UIStates.Gameplay);
        OnPauseRequested?.Invoke(); // GameManager toggles the pause, so this only unpauses while paused.
    }

    /// <summary>Asks GameManager to start the game.</summary>
    public void StartButtonPressed()
    {
        OnStartRequested?.Invoke();
    }

    /// <summary>Asks GameManager to go to the main menu.</summary>
    public void MenuButtonPressed()
    {
        OnMenuRequested?.Invoke();
    }

    /// <summary>Asks GameManager to close the game.</summary>
    public void QuitButtonPressed()
    {
        OnExitRequested?.Invoke();
    }

    /// <summary>Asks GameManager to return to the Hub.</summary>
    public void ResetButtonPressed()
    {
        OnResetRequested?.Invoke();
    }

    /// <summary>Hides the other screens and shows Settings.</summary>
    public void SettingsButtonPressed()
    {
        HideAllScreens();
        ShowScreen(_settingsScreen);
    }

    #endregion

    /// <summary>Shows the death screen. Runs on GameManager.OnGameLost, and as a debug button.</summary>
    // INTEGRATION: the J debug key became the Inspector button.
    [Button, DisableInEditorMode]
    private void GameOver()
    {
        HideAllScreens();
        ShowScreen(_deathScreen);
    }

    /// <summary>Shows the win screen. Runs on GameManager.OnGameWon, and as a debug button.</summary>
    // INTEGRATION: the M debug key became the Inspector button.
    [Button, DisableInEditorMode]
    private void GameWin()
    {
        HideAllScreens();
        ShowScreen(_winScreen);
    }

    private void HandleRewards(Room room)
    {
        Debug.Log("HandleRewards");
        if (room.RewardOptions.Count == 0)
        {
            Debug.Log("no reward options");
            return;
        }
        ChangeState(UIStates.RewardChoice);
        rewardScreen.ShowChoices(room, room.RewardOptions);
    }

    /// <summary>
    /// Shows any given UI screen and handles cursor visiblity
    /// </summary>
    /// <param name="givenScreen">Which screen to be shown</param>
    public void ShowScreen(UIScreen givenScreen) 
    /// <summary>Shows a screen and sets the cursor to match it.</summary>
    /// <param name="givenScreen">Which screen to show.</param>
    public void ShowScreen(UIScreen givenScreen)
    {
        givenScreen.Show();
        ToggleCursor(givenScreen.ShowsCursor);
    }

    /// <summary>Hides a screen. The cursor isn't changed.</summary>
    public void HideScreen(UIScreen givenScreen)
    {
        givenScreen.Hide();
    }

    /// <summary>Hides every screen. The HUD stays up unless hideHUD is true.</summary>
    // INTEGRATION: the hideHUD option is new. HUDScreen is now a UIScreen, so it's in _screenList, and switching screens
    // would otherwise hide it.
    public void HideAllScreens(bool hideHUD = false)
    {
        foreach (UIScreen screen in _screenList)
        {
            if (hideHUD || screen is not HUDScreen)
            {
                screen.Hide();
            }
        }
    }

    #region misc

    /// <summary>Shows and frees the cursor, or hides and locks it. Called by ShowScreen and ChangeState.</summary>
    private void ToggleCursor(bool isVisible)
    {
        if (!isVisible)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            _cursorVisible = false;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            _cursorVisible = true;
        }
    }

    /// <summary>Shows the pause menu when paused, and returns to gameplay when unpaused. Runs on GameManager.OnPauseToggled.</summary>
    private void HandlePause(bool isPaused)
    {
        if (isPaused)
        {
            ChangeState(UIStates.PauseMenu);
        }
        else
        {
            ChangeState(UIStates.Gameplay);
            ToggleCursor(false);
        }
    }

    #endregion

    /// <summary>
    /// Matches the UI to GameManager's state: the start menu in Menu, gameplay in Hub and Playing. Dead and Win are
    /// handled by GameOver and GameWin. Runs on GameManager.OnGameStateChanged.
    /// </summary>
    private void HandleStateChanged(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameManager.GameState.Menu:
                ChangeState(UIStates.StartMenu);
                break;
            case GameManager.GameState.Hub:
                ChangeState(UIStates.Gameplay);
                break;
            case GameManager.GameState.Playing:
                ChangeState(UIStates.Gameplay);
                break;
            case GameManager.GameState.Dead:
                break;
            case GameManager.GameState.Win:
                break;
            case GameManager.GameState.Store:
                break;
        }
    }

    /// <summary>Stops listening to GameManager.</summary>
    // INTEGRATION: new. UIManager now persists, but a duplicate destroyed by MGR still needs to unsubscribe.
    private void OnDestroy()
    {
        GameManager.OnGameLost -= GameOver;
        GameManager.OnGameWon -= GameWin;
        GameManager.OnPauseToggled -= HandlePause;
        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnGameStateChanged -= HandleStateChanged;
        }
    }
}
