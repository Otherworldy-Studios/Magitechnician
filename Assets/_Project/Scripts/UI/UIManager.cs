using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using _Project.Scripts.Dungeon;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using _Project.Scripts.Dungeon;



public class UIManager : MGR<UIManager>
{

   
    private enum UIStates
    {
        StartMenu,
        PauseMenu,
        Gameplay,
        RewardChoice
    }

    [SerializeField] private List<UIScreen> screenList;
    [SerializeField] private MenuScreen menuScreen;
    [SerializeField] private RewardScreen rewardScreen;
    [SerializeField] private SettingsScreen settingsScreen;
    [SerializeField] private DeathScreen deathScreen;
    [SerializeField] private WinScreen winScreen;
    [SerializeField] private PauseScreen pauseScreen;
    [SerializeField] private UIStates currentState;

    public static event Action OnPauseRequested;
    public static event Action OnStartRequested;
    public static event Action OnResetRequested;
    public static event Action OnMenuRequested; 
    public static event Action OnExitRequested; 
    
    private UIScreen _activeScreen;
    private bool _cursorVisible;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
        {
            return;
        }
        screenList = new List<UIScreen>(GetComponentsInChildren<UIScreen>(true));
        HideAllScreens(); //makes sure the screens are 
        if(SceneManager.GetActiveScene().buildIndex != Scenes.MainMenu)
        {
            ChangeState(UIStates.Gameplay);
            HideAllScreens();
        }
        GameManager.OnGameOver += GameOver;
        GameManager.OnGameWin += GameWin;
        GameManager.OnPauseToggle += HandlePause;
        GameManager.Instance.OnGameStateChanged += HandleStateChanged;
        Room.OnRoomCleared += HandleRewards;

    }

    [Button, DisableInEditorMode]
    private void DEBUGChangeUIState(UIStates state)
    {
        ChangeState(state);
    }
    
    private void ChangeState(UIStates state)
    {
        currentState = state;
        switch (state)
        {
            case UIStates.StartMenu:
                HideAllScreens();
                ShowScreen(menuScreen);
                break;
            case UIStates.PauseMenu:
                HideAllScreens();
                ShowScreen(pauseScreen);
                break;
            case UIStates.Gameplay:
                HideAllScreens();
                ToggleCursor(false);
                //connecting player UI and the UI manager here might cause issues with not showing it
                break;
            case UIStates.RewardChoice:
                HideAllScreens();
                ShowScreen(rewardScreen);
                break;
        }
    }

    #region ButtonHandlers

    public void ResumeButtonPressed()
    {
        ChangeState(UIStates.Gameplay);
        OnPauseRequested?.Invoke(); //toggle bug is possible however it SHOULD be handled by the GM
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
    
    /// <summary>
    /// To hub
    /// </summary>
    public void ResetButtonPressed()
    { 
        OnResetRequested?.Invoke();
    }

    public void SettingsButtonPressed()
    {
        HideAllScreens();
        ShowScreen(settingsScreen);
    }

    #endregion

    [Button, DisableInEditorMode]
    private void GameOver()
    {
        HideAllScreens();
        ShowScreen(deathScreen);
    }

    [Button, DisableInEditorMode]
    private void GameWin()
    {
        HideAllScreens();
        ShowScreen(winScreen);
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
    {
        givenScreen.Show();
        ToggleCursor(givenScreen.showsCursor);
    }

    public void HideScreen(UIScreen givenScreen) ////requirements say this should be in ShowScreen but i feel like that's less powerful
    {
        givenScreen.Hide();
    }

    
    public void HideAllScreens(bool hideHUD = false)
    {
        foreach (UIScreen screen in screenList)
        {
            if (hideHUD || screen is not HUDScreen)
            {
                screen.Hide();
            }
        }
    }

    
    #region misc
    
    /// <summary>
        /// Toggle cursor visibility, used implicitly when changing screens
        /// </summary>
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
    
        /// <summary>
        /// Called when GM invokes the pause request
        /// </summary>
        /// <param name="isPaused"></param>
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
                //do we need a separate event listener for win/loss if this exists?
                break;
            case GameManager.GameState.Win:
                break;
            case GameManager.GameState.Store:
                break;
        }
    }


    private void OnDestroy()
    {
        GameManager.OnGameOver -= GameOver;
        GameManager.OnGameWin -= GameWin;
        GameManager.OnPauseToggle -= HandlePause;
        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnGameStateChanged -= HandleStateChanged;
        }
    }

}
