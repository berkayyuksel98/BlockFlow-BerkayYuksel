using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

// Arayüz panellerinin açılıp kapanmasını ve geçişlerini merkezi olarak yöneten sınıf
public class UIManager : IInitializable, IDisposable
{
    private readonly IEventBus eventBus;
    private readonly ILevelManager levelManager;
    private readonly GameplayPanel gameplayPanel;
    private readonly WinPanel winPanel;
    private readonly LosePanel losePanel;
    private readonly System.Threading.CancellationTokenSource cts = new();

    [Inject]
    public UIManager(IEventBus eventBus, ILevelManager levelManager,
                     GameplayPanel gameplayPanel, WinPanel winPanel, LosePanel losePanel)
    {
        this.eventBus = eventBus;
        this.levelManager = levelManager;
        this.gameplayPanel = gameplayPanel;
        this.winPanel = winPanel;
        this.losePanel = losePanel;
    }

    public void Initialize()
    {
        eventBus.Subscribe<LevelLoadedEvent>(OnLevelLoaded);
        eventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);

        winPanel.Hide();
        losePanel.Hide();
        gameplayPanel.Show(levelManager.CurrentLevelIndex);
    }

    public void Dispose()
    {
        eventBus?.Unsubscribe<LevelLoadedEvent>(OnLevelLoaded);
        eventBus?.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
        cts.Cancel();
        cts.Dispose();
    }

    private void OnLevelLoaded(LevelLoadedEvent e) => ShowGameplayPanel(e.LevelIndex);

    private void OnLevelCompleted(LevelCompletedEvent e) => ShowResultPanel(e.IsWin);

    public void ShowGameplayPanel(int levelIndex)
    {
        winPanel.Hide();
        losePanel.Hide();
        gameplayPanel.Show(levelIndex);
    }

    public void ShowResultPanel(bool isWin)
    {
        ShowResultPanelDelayed(isWin).Forget();
    }

    private async UniTaskVoid ShowResultPanelDelayed(bool isWin)
    {
        await UniTask.WaitForSeconds(1f, ignoreTimeScale: true, cancellationToken: cts.Token);
        gameplayPanel.Hide();
        if (isWin) { losePanel.Hide(); winPanel.Show(); }
        else { winPanel.Hide(); losePanel.Show(); }
    }

    public void HideAllPanels()
    {
        gameplayPanel.Hide();
        winPanel.Hide();
        losePanel.Hide();
    }
}

