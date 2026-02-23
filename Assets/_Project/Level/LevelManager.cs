using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using Zenject;

// Level yaşam döngüsünü yöneten sınıf — JSON yükleme, timer ve GridManager koordinasyonu
public class LevelManager : IInitializable, IDisposable, ILevelManager
{
    // Level indeksi PlayerPrefs'te kalıcı olarak saklanır
    public int CurrentLevelIndex
    {
        get
        {
            if (!PlayerPrefs.HasKey("LevelIndex"))
                PlayerPrefs.SetInt("LevelIndex", 0);

            return PlayerPrefs.GetInt("LevelIndex");
        }
        private set
        {
            PlayerPrefs.SetInt("LevelIndex", value);
        }
    }

    public LevelData CurrentLevel { get; private set; }

    private float remainingTime;
    private bool isTimerRunning;
    private CancellationTokenSource cancellationTokenSource;
    private int remainingBlockCount;

    private readonly GridManager gridManager;
    private readonly IEventBus eventBus;
    private readonly GameConfig gameConfig;

    [Inject]
    public LevelManager(GridManager gridManager, IEventBus eventBus, GameConfig gameConfig)
    {
        this.gridManager = gridManager;
        this.eventBus    = eventBus;
        this.gameConfig  = gameConfig;
    }


    public void Initialize()
    {
        eventBus.Subscribe<BlockExitedEvent>(OnBlockExited);
        LoadLevel(CurrentLevelIndex);
    }

    public void LoadLevel(int index)
    {
        LoadLevelAsync(index).Forget();
    }

    private async UniTaskVoid LoadLevelAsync(int index)
    {
        StopTimer();

        // Gerçek veri indeksi: level listesi bitince başa döner
        int dataIndex = gameConfig.Levels.Count > 0 ? index % gameConfig.Levels.Count : 0;
        TextAsset jsonFile = gameConfig.Levels.Count > 0 ? gameConfig.Levels[dataIndex] : null;

        if (jsonFile == null)
        {
            Debug.LogError($"[LevelManager] Level dosyası bulunamadı: GameConfig.Levels[{dataIndex}]");
            return;
        }

        CurrentLevel = JsonUtility.FromJson<LevelData>(jsonFile.text);
        CurrentLevelIndex = index;

        // Blok spawn animasyonları bitene kadar bekle
        await gridManager.BuildLevel(CurrentLevel);
        remainingBlockCount = CurrentLevel.Blocks.Count;
        eventBus.Publish(new LevelLoadedEvent { LevelIndex = CurrentLevelIndex });
        StartTimer(CurrentLevel.TimeLimit).Forget();
    }

    public void LoadNextLevel()
    {
        LoadLevel(CurrentLevelIndex + 1);
    }

    public void ReloadCurrentLevel()
    {
        LoadLevel(CurrentLevelIndex);
    }

    // Geri sayım timerını UniTask ile başlatır; süre bitince TimerExpiredEvent yayınlar
    private async UniTaskVoid StartTimer(float duration)
    {
        if (duration <= 0f) return; // Süresiz level

        cancellationTokenSource = new CancellationTokenSource();
        remainingTime = duration;
        isTimerRunning = true;

        try
        {
            while (remainingTime > 0f)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationTokenSource.Token);
                remainingTime -= Time.deltaTime;
            }

            isTimerRunning = false;
            eventBus.Publish(new TimerExpiredEvent());
        }
        catch (System.OperationCanceledException)
        {
            // Timer durduruldu (level yeniden yüklendi veya uygulama kapandı) — normal akış
            isTimerRunning = false;
        }
    }
    private void StopTimer()
    {
        isTimerRunning = false;
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
        cancellationTokenSource = null;
    }

    private void OnBlockExited(BlockExitedEvent e)
    {
        remainingBlockCount--;
        if (remainingBlockCount <= 0)
        {
            StopTimer();
            eventBus.Publish(new LevelCompletedEvent
            {
                LevelIndex    = CurrentLevelIndex,
                IsWin         = true,
                RemainingTime = GetRemainingTime(),
            });
        }
    }

    public void Dispose()
    {
        eventBus?.Unsubscribe<BlockExitedEvent>(OnBlockExited);
        StopTimer();
    }

    public float GetRemainingTime() => Mathf.Max(remainingTime, 0f);
}

