using UnityEngine;
using System;
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

    [SerializeField] private UIScreen[] screenList;
    [SerializeField] private MenuScreen menuScreen;
    private bool _cursorVisible;

    private void Awake()
    {
        //get all uiscreens in prefab
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
                ShowScreen(menuScreen);
                break;
            case UIStates.PauseMenu:
                break;
            case UIStates.Gameplay:
                break;
        }
    }
    
    private void PauseButtonPressed()
    {
        GameManager.Instance.TogglePause();
    }

    private void StartButtonPressed()
    {
        GameManager.Instance.StartHub();
    }

    private void ResetButtonPressed()
    {
        GameManager.Instance.ResetGame();
    }

    private void GameOver()
    {
        //show gameover screen
    }

    private void GameWin()
    {
        //show gamewin screen
    }

    private void ShowScreen(UIStates givenScreen)
    {
        givenScreen..Enable();
    }

    
    private void HideAllScreens()
    {
        foreach (UIScreen screen in screenList)
        {
            //screen.(screenpart).Disable();
        }
    }

    /// <summary>
    /// Toggle cursor visibility
    /// </summary>
    private void ToggleCursor(bool isVisible)
    {
        if (!isVisible)
        {
           Cursor.visible = false; 
           _cursorVisible = false;
        }
        else
        {
            Cursor.visible = true; 
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
            ToggleCursor(true);
            //show pause screen and menu
        }
        else
        {
            ToggleCursor(false);
            //hide the pausescreen
        }
        
    }

    private void HandleStateChanged(GameManager.GameState newState)
    {
        switch (newState)
        {
            
        }
    }
    

}
