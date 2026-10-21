using System;
using System.Collections;
using _Project.Scripts.LevelGenerator;
using Unity.VisualScripting;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Menu,
        Hub,
        Playing,
        Dead, //game over
        Win
    }

    public enum RunResult
    {
        Victory,
        Lose,
    }
    public static GameManager Instance { get; private set; } //assumed GameManager persists between scenes
    
    public static event Action OnGameOver;
    public static event Action OnGameWin;
    public static event Action OnGenerateRequested; //an event so that the UI can listen and add a loading thing
    public static event Action OnRunStart;
    public static event Action<bool> OnPauseToggle;
    public event Action<GameState> OnGameStateChanged; 
    
    public float StartTime { get; private set; } //MUST BE EQUALIZED
    public float EndTime { get; private set; }
   
    public float CurrentRoom { get; private set; }
    public float RunTime => Time.time - StartTime;
    
    [SerializeField] private GameState currentState; //serialized for viewing in editor
    [SerializeField] private PH_Player playerCharacter;
    [SerializeField] private Room[,] currentDungeon;
    private bool _isPaused;
    private float _runTime;
    //should rooms carry their own timer? i think so 
    
    private void Awake()
    {
        Instance = this;
        currentState = GameState.Menu;
    }

    private void Start()
    {
        PH_UIManager.OnPauseRequested += TogglePause;
        PH_UIManager.OnResetRequested += ResetGame;
        PH_UIManager.OnStartRequested += StartHub;
        PH_LevelGenerator.OnMapGenerated += StartRun;
        PH_Player.OnPlayerDeath += PlayerDied;
    }

    public float GetRunTime()
    {
        _runTime = Time.time - StartTime; //necessary step to equalize the Time.time
        return _runTime;
    }

    public void RequestGameEnd(RunResult result)
    {
        if (currentState != GameState.Playing)
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

    private void PlayerDied() //i personally prefer having these simple named methods for events rather than lambdaing it to 
    {
        RequestGameEnd(RunResult.Lose);
    }

    #region StateBackbone

    private void ChangeState(GameState newState)
        {
            if (currentState == newState)
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
            }
            OnGameStateChanged?.Invoke(currentState); 
        }
    
    private void EnterPlaying()
        {
            StartTime = Time.time; //start time is equalized to 0, showing it will be a crazy ass number
            //give player full control over their character n such, systems all working
            
        }
        private void EnterMenu()
        {
            //probably some code here about spawning back in the menu and despawning everything
        }
    
        private void EnterHub()
        {
            
        }
    
        private void EnterDead()
        {
            CaptureTime();
            OnGameOver?.Invoke();
        }
    
        private void EnterWin()
        {
            CaptureTime();
            OnGameWin?.Invoke();
        }

    #endregion
    
    private void InitializeRun() //unsure how to trigger this
    {
        OnGenerateRequested?.Invoke(); 
    }

    private void StartHub()
    {
        ChangeState(GameState.Hub);
    }

    private void StartRun()
    {
        ChangeState(GameState.Playing);
        //teleport player function should be its own script?
        //get 1st room reference
        OnRunStart?.Invoke();
    }
    
    #region Misc
    
    private void TogglePause()
        {
            if (currentState != GameState.Playing) 
            {
                return;
            }
    
            if (_isPaused)
            {
                Time.timeScale = 1;
                _isPaused = false;
            }
            else
            {
                Time.timeScale = 0;
                _isPaused = true;
            }
            OnPauseToggle?.Invoke(_isPaused);
        }
    
    
    #endregion
    
    

    private void ResetGame()
    {
        if (currentState == GameState.Win || currentState == GameState.Dead) //only allow the reset request to work on win/loss
        {
           ChangeState(GameState.Hub); 
        }
    }

    #region stats

    private void CaptureTime() //for the end of the run for stats
    {
        EndTime = Time.time - StartTime;
        //some extra time adding up all the room times? do this when the rooms are up
    }
    
    //EnemiesKilled
    //DamageTaken

    
    #endregion
    
    
    //track player room
    
    //subscribe to room on enter event, wavemanager will probably also use that
    
    
    
}

#region PLACEHOLDERS

public class PH_UIManager : MonoBehaviour
{
    public static event Action OnPauseRequested;
    public static event Action OnStartRequested;
    public static event Action OnResetRequested;

    
    private void Start()
    {
        GameManager.OnGameOver += GameOver;
        GameManager.OnGameWin += GameWin;
    }
    
    
    private void PauseButtonPressed()
    {
        OnPauseRequested?.Invoke();
    }

    public void StartButtonPressed()
    {
        OnStartRequested?.Invoke();
    }

    private void GameOver()
    {
        
    }

    private void GameWin()
    {
        
    }
    
}
public class PH_RoomManager : MonoBehaviour //each room cares about its own wave?
{
    public static event Action OnRoomEntered;
}
public class PH_LevelGenerator : MonoBehaviour
{
    public static event Action OnMapGenerated;
}

public class PH_Player : MonoBehaviour
{
    public static event Action OnPlayerDeath;
    private void OnPlayerDied()
    {
        OnPlayerDeath?.Invoke();
    }
}

#endregion