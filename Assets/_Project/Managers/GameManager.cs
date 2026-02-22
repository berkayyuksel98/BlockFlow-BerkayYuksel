using System;
using UnityEngine;
using Zenject;

// Oyunun genel state makinesini ve kazanma/kaybetme döngülerini yöneten sınıf
// EventBus üzerinden LevelCompletedEvent ve TimerExpiredEvent'i dinler
public class GameManager : IInitializable, IDisposable
{
    public GameState CurrentState { get; private set; } = GameState.Idle;

    private readonly IEventBus eventBus;
    private readonly ILevelManager levelManager;


    [Inject]
    public GameManager(IEventBus eventBus, ILevelManager levelManager)
    {
        this.eventBus = eventBus;
        this.levelManager = levelManager;
    }


    public void Initialize()
    {
        RegisterEvents();
        SetState(GameState.Playing);
    }


    public void Dispose()
    {
        UnregisterEvents();
    }

    #region  Public API
    public void StartGame()
    {
        if (CurrentState == GameState.Idle)
            SetState(GameState.Playing);
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Playing) return;
        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused) return;
        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SetState(GameState.Playing);
        levelManager.ReloadCurrentLevel();
    }

    public void GoToNextLevel()
    {
        Time.timeScale = 1f;
        SetState(GameState.Playing);
        levelManager.LoadNextLevel();
    }
    #endregion
    #region  Events

    private void RegisterEvents()
    {
        eventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
        eventBus.Subscribe<TimerExpiredEvent>(OnTimerExpired);
    }

    private void UnregisterEvents()
    {
        eventBus?.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
        eventBus?.Unsubscribe<TimerExpiredEvent>(OnTimerExpired);
    }
    private void OnLevelCompleted(LevelCompletedEvent e)
    {
        Time.timeScale = 0f;
        SetState(e.IsWin ? GameState.Win : GameState.Lose);
    }

    private void OnTimerExpired(TimerExpiredEvent e)
    {
        if (CurrentState != GameState.Playing) return;

        eventBus.Publish(new LevelCompletedEvent
        {
            LevelIndex = levelManager.CurrentLevelIndex,
            IsWin = false,
            RemainingTime = 0f
        });
    }
    #endregion

    private void SetState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log($"[GameManager] State → {newState}");
    }
}
