using UnityEngine;
using System;
using UnityEngine.XR;

public class UIManager : MonoBehaviour
{
    
    private void Start()
    {
        GameManager.OnGameOver += GameOver;
        GameManager.OnGameWin += GameWin;
        GameManager.OnPauseToggle += HandlePause;
        GameManager.OnGameStateChanged += ;
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

    private void ShowScreen()
    {
        
    }

    private void HideAllScreens()
    {
        
    }

    private void HandlePause(bool isPaused)
    {
        //show pause screen and menu
    }

    private void HandleStateChanged()
    {
        
    }

}
