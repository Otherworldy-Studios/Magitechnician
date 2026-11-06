using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;

public class UIManager : MonoBehaviour
{

    public static UIManager instance {get; private set;}
    private enum UIStates
    {
        StartMenu,
        PauseMenu,
        Gameplay
    }

    [SerializeField] private List<UIScreen> screenList;
    [SerializeField] private MenuScreen menuScreen;
    [SerializeField] private RewardScreen rewardScreen;
    [SerializeField] private SettingsScreen settingsScreen;
    [SerializeField] private DeathScreen deathScreen;
    [SerializeField] private WinScreen winScreen;
    [SerializeField] private PauseScreen pauseScreen;

    public static event Action OnPauseRequested;
    public static event Action OnStartRequested;
    public static event Action OnResetRequested;
    public static event Action OnMenuRequested; //NEW EVENT, SEND TO MENU NEEDED IN GM
    public static event Action OnExitRequested; //NEW EVENT, Applicaiton.Quit() needed in GM
    
    private UIScreen _activeScreen;
    private bool _cursorVisible;

    private void Awake()
    {
        instance = this;
        screenList = new List<UIScreen>(GetComponentsInChildren<UIScreen>(true));
        HideAllScreens(); //makes sure the screens are 
    }

    private void Start()
    {
        GameManager.OnGameOver += GameOver;
        GameManager.OnGameWin += GameWin;
        GameManager.OnPauseToggle += HandlePause;
        //GameManager.OnGameStateChanged += HandleStateChanged; COMMENTED FOR DEBUG ///TEMPORARY
        GameManager.Instance.OnGameStateChanged += HandleStateChanged;

    }

    private void Update()
    {
        //DEBUG i could make this a switch at this point but idc its debug
        #if UNITY_EDITOR
        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            GameOver();
        }
        else if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            GameWin();
        }
        else if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            ChangeState(UIStates.StartMenu);
        }
        #endif
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
                ToggleCursor(true);
                //connecting player UI and the UI manager here might cause issues with not showing it
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
    

    private void GameOver()
    {
        HideAllScreens();
        ShowScreen(deathScreen);
    }

    private void GameWin()
    {
        HideAllScreens();
        ShowScreen(winScreen);
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

    
    public void HideAllScreens()
    {
        foreach (UIScreen screen in screenList)
        {
            screen.Hide();
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
    

}
