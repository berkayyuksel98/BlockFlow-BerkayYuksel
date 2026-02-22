using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using Zenject;

// Level yaşam döngüsünü yöneten sınıf — JSON yükleme, timer ve GridManager koordinasyonu
public class LevelManager : IInitializable, ILevelManager
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

    private readonly GridManager gridManager;
    private readonly IEventBus eventBus;
    private const string LevelResourcePath = "Levels/Level_";


    [Inject]
    public LevelManager(GridManager gridManager, IEventBus eventBus)
    {
        this.gridManager = gridManager;
        this.eventBus = eventBus;
    }


    public void Initialize()
    {
        LoadLevel(CurrentLevelIndex);
    }

    public void LoadLevel(int index)
    {
        StopTimer();

        string path = LevelResourcePath + index;
        TextAsset jsonFile = Resources.Load<TextAsset>(path);

        if (jsonFile == null)
        {
            Debug.LogError($"[LevelManager] Level dosyası bulunamadı: Resources/{path}.json");
            return;
        }

        CurrentLevel = JsonUtility.FromJson<LevelData>(jsonFile.text);
        CurrentLevelIndex = index;

        gridManager.BuildLevel(CurrentLevel); // GridManager'a level verisini göndererek sahneyi kurmasını sağlar
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
    public float GetRemainingTime() => Mathf.Max(remainingTime, 0f);
}

