using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shows and hides the UI screens, and sets the cursor for each. Screen buttons call the ButtonPressed methods, which
/// raise static request events that GameManager handles. Listens to GameManager for game over, win, pause, and state
/// changes.
/// </summary>
public class UIManager : MGR<UIManager>
{
    /// <summary>Which group of screens is showing.</summary>
    private enum UIStates
    {
        StartMenu,
        PauseMenu,
        Gameplay,
        Reward,
        Shop,
        Charm,
        ChestLoot
    }

    [Tooltip("Filled from the children in Awake.")]
    [SerializeField] private List<UIScreen> _screenList;
    [SerializeField] private MenuScreen _menuScreen;
    [SerializeField] private RewardScreen _rewardScreen;
    [SerializeField] private SettingsScreen _settingsScreen;
    [SerializeField] private DeathScreen _deathScreen;
    [SerializeField] private WinScreen _winScreen;
    [SerializeField] private PauseScreen _pauseScreen;
    [SerializeField] private ShopScreen _shopScreen;
    [SerializeField] private CharmScreen _charmScreen;
    [SerializeField] private ChestLootScreen _chestLootScreen;
    [Tooltip("Found among the child screens if empty.")]
    [SerializeField] private HUDScreen _hudScreen;
    [Tooltip("Shown in the Inspector for debugging. Don't set it by hand.")]
    [SerializeField] private UIStates _currentState;

    /// <summary>Resume was pressed. GameManager toggles the pause.</summary>
    public static event Action<bool> OnPauseRequested;
    /// <summary>Start was pressed on the main menu. GameManager loads the Hub.</summary>
    public static event Action OnStartRequested;
    /// <summary>The shop or charm screen opened. GameManager enters Store.</summary>
    public static event Action OnStoreRequested;
    /// <summary>Restart was pressed. GameManager returns to the Hub.</summary>
    public static event Action OnResetRequested;
    /// <summary>Main Menu was pressed. GameManager loads the main menu.</summary>
    public static event Action OnMenuRequested;
    /// <summary>Quit was pressed. GameManager closes the game.</summary>
    public static event Action OnExitRequested;

    /// <summary>True while the reward screen or the chest loot screen is open. Escape doesn't pause during either.</summary>
    public bool IsRewardOpen => _currentState == UIStates.Reward || _currentState == UIStates.ChestLoot;

    protected override void Awake()
    {
        // MGR.Awake sets Instance and destroys duplicates, so a second UIManager from the next scene stops here.
        base.Awake();
        if (Instance != this) return;

        _screenList = new List<UIScreen>(GetComponentsInChildren<UIScreen>(true));
        FindHUD();
        HideAllScreens();

        // Starting Play mode in any scene other than the main menu goes straight to gameplay, which locks the cursor.
        if (SceneManager.GetActiveScene().buildIndex != Scenes.MainMenu)
        {
            ChangeState(UIStates.Gameplay);
            HideAllScreens();
        }

        GameManager.OnGameLost += GameOver;
        GameManager.OnGameWon += GameWin;
        GameManager.OnPauseToggled += HandlePause;
        GameManager.Instance.OnGameStateChanged += HandleStateChanged;
    }

    // A duplicate destroyed by MGR still needs to unsubscribe.
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

    /// <summary>Hides every screen except the HUD and shows the ones for the new state.</summary>
    private void ChangeState(UIStates state)
    {
        _currentState = state;
        switch (state)
        {
            case UIStates.StartMenu:
                HideAllScreens();
                SetHUDVisible(false);
                ShowScreen(_menuScreen);
                break;
            case UIStates.PauseMenu:
                HideAllScreens();
                SetHUDVisible(false);
                ShowScreen(_pauseScreen);
                break;
            case UIStates.Gameplay:
                HideAllScreens();
                SetHUDVisible(true);
                ToggleCursor(false);
                break;
            case UIStates.Reward:
                HideAllScreens();
                SetHUDVisible(true);
                ShowScreen(_rewardScreen);
                OnPauseRequested?.Invoke(true);
                break;
            case UIStates.Shop:
                HideAllScreens();
                SetHUDVisible(true);
                ShowScreen(_shopScreen);
                break;
            case UIStates.Charm:
                HideAllScreens();
                SetHUDVisible(true);
                ShowScreen(_charmScreen);
                break;
            case UIStates.ChestLoot:
                HideAllScreens();
                SetHUDVisible(true);
                ShowScreen(_chestLootScreen);
                OnPauseRequested?.Invoke(true);
                break;
        }
    }

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
            case GameManager.GameState.Playing:
                ChangeState(UIStates.Gameplay);
                break;
        }
    }

    #region Button Handlers

    /// <summary>Returns to gameplay and asks GameManager to unpause.</summary>
    public void ResumeButtonPressed()
    {
        OnPauseRequested?.Invoke(false);
        ChangeState(UIStates.Gameplay);
    }

    public void StartButtonPressed()
    {
        OnStartRequested?.Invoke();
    }

    public void MenuButtonPressed()
    {
        OnMenuRequested?.Invoke();
    }

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

    /// <summary>Leaves Settings by showing the current state's screens again.</summary>
    public void BackButtonPressed()
    {
        ChangeState(_currentState);
    }

    #endregion

    #region Screens

    /// <summary>Opens the reward screen with the rolled parts, unless there are none.</summary>
    public void HandleRewards(IReadOnlyList<WeightedReward<GunPart>> rewards)
    {
        if (rewards.Count == 0) return;

        ChangeState(UIStates.Reward);
        _rewardScreen.ShowChoices(rewards);
    }

    /// <summary>Opens the chest loot screen with what a chest gave the player. Pauses the game until it closes.</summary>
    public void ShowChestLoot(int crystals, Consumable consumable, bool consumableAdded)
    {
        if (_chestLootScreen == null) return;

        ChangeState(UIStates.ChestLoot);
        _chestLootScreen.ShowLoot(crystals, consumable, consumableAdded);
    }

    [Button, DisableInEditorMode]
    public void OpenShop()
    {
        OnStoreRequested?.Invoke();
        ChangeState(UIStates.Shop);
    }

    [Button, DisableInEditorMode]
    public void OpenCharmScreen()
    {
        OnStoreRequested?.Invoke();
        ChangeState(UIStates.Charm);
    }

    /// <summary>Closes the shop or charm screen through GameManager. Other screens stay open.</summary>
    public void CloseScreen()
    {
        if (_currentState != UIStates.Shop && _currentState != UIStates.Charm) return;

        GameManager.Instance.CloseState();
    }

    /// <summary>Shows a screen and sets the cursor to match it.</summary>
    /// <param name="givenScreen">Which screen to be shown</param>
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

    /// <summary>Shows the death screen. Runs on GameManager.OnGameLost, and as a debug button.</summary>
    [Button, DisableInEditorMode]
    private void GameOver()
    {
        HideAllScreens();
        ShowScreen(_deathScreen);
    }

    /// <summary>Shows the win screen. Runs on GameManager.OnGameWon, and as a debug button.</summary>
    [Button, DisableInEditorMode]
    private void GameWin()
    {
        HideAllScreens();
        ShowScreen(_winScreen);
    }

    /// <summary>Uses the HUDScreen among the children when none is assigned.</summary>
    private void FindHUD()
    {
        if (_hudScreen != null) return;

        foreach (UIScreen screen in _screenList)
        {
            if (screen is HUDScreen hud)
            {
                _hudScreen = hud;
                return;
            }
        }

        Debug.LogWarning("UIManager has no HUDScreen. Assign one in the Managers prefab.", this);
    }

    private void SetHUDVisible(bool visible)
    {
        if (_hudScreen == null) return;

        if (visible)
        {
            _hudScreen.Show();
        }
        else
        {
            _hudScreen.Hide();
        }
    }

    #endregion

    #region Misc

    /// <summary>
    /// Toggle cursor visibility, used implicitly when changing screens
    /// </summary>
    private void ToggleCursor(bool isVisible)
    {
        Cursor.visible = isVisible;
        Cursor.lockState = isVisible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    /// <summary>
    /// Called when GM invokes the pause request
    /// </summary>
    private void HandlePause(bool isPaused)
    {
        if (_currentState == UIStates.Reward || _currentState == UIStates.ChestLoot ||
            _currentState == UIStates.Charm || _currentState == UIStates.Shop) return;

        if (isPaused)
        {
            ChangeState(UIStates.PauseMenu);
        }
        else
        {
            ChangeState(UIStates.Gameplay);
        }
    }

    /// <summary>Debug: switches to a UI state.</summary>
    [Button, DisableInEditorMode]
    private void DEBUGChangeUIState(UIStates state)
    {
        ChangeState(state);
    }

    #endregion
}