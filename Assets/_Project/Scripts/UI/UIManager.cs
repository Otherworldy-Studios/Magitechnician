using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.UI;
using UnityEngine.XR;

public class UIManager : MonoBehaviour
{

    private enum UIStates
    {
        StartMenu,
        PauseMenu,
        Gameplay
    }

    [SerializeField] private List<UIScreen> screenList;
    [SerializeField] private MenuScreen menuScreen;

    public event Action OnPauseRequested;
    public event Action OnStartRequested;
    public event Action OnResetRequested;
    
    private UIScreen _activeScreen;
    private bool _cursorVisible;

    private void Awake()
    {
        //get all uiscreens in prefab
        screenList = new List<UIScreen>(GetComponentsInChildren<UIScreen>(true));
        HideAllScreens(); //makes sure the screens are 
    }

    private void Start()
    {
        GameManager.OnGameOver += GameOver;
        GameManager.OnGameWin += GameWin;
        GameManager.OnPauseToggle += HandlePause;
        GameManager.OnGameStateChanged += HandleStateChanged;
    }
    
    private void ChangeState(UIStates state)
    {
        switch (state)
        {
            case UIStates.StartMenu:
                HideAllScreens();
                //ShowScreen(Menu);
                break;
            case UIStates.PauseMenu:
                HideAllScreens();
                //ShowScreen(Pause);
                break;
            case UIStates.Gameplay:
                HideAllScreens();
                break;
        }
    }

    #region ButtonHandlers

    private void PauseButtonPressed()
    {
        OnPauseRequested?.Invoke();
    }
    
    private void StartButtonPressed()
    {
        OnStartRequested?.Invoke();
    }
    
    private void ResetButtonPressed()
    { 
        OnResetRequested?.Invoke();
    }
    
    #endregion
    

    private void GameOver()
    {
        
    }

    private void GameWin()
    {
        //show gamewin screen
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
