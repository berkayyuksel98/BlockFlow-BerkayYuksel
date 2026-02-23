using System;
using UnityEngine;
using Zenject;

public enum AudioType
{
    DragStart,
    DragEnd,
    BlockExit,
    LevelWin,
    LevelLose,
}

// EventBus üzerinden gelen eventlere göre ses efektlerini oynatan pure C# servis
public class AudioController : IInitializable, IDisposable
{
    private readonly IEventBus eventBus;
    private readonly AudioConfig audioConfig;
    private readonly AudioSource audioSource;

    [Inject]
    public AudioController(IEventBus eventBus, AudioConfig audioConfig, Camera camera)
    {
        this.eventBus = eventBus;
        this.audioConfig = audioConfig;
        audioSource = camera.GetComponent<AudioSource>();
    }

    public void Initialize()
    {
        eventBus.Subscribe<DragStartedEvent>(OnDragStarted);
        eventBus.Subscribe<DragEndedEvent>(OnDragEnded);
        eventBus.Subscribe<BlockExitStartedEvent>(OnBlockExited);
        eventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
    }

    public void Dispose()
    {
        eventBus?.Unsubscribe<DragStartedEvent>(OnDragStarted);
        eventBus?.Unsubscribe<DragEndedEvent>(OnDragEnded);
        eventBus?.Unsubscribe<BlockExitStartedEvent>(OnBlockExited);
        eventBus?.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
    }

    private void OnDragStarted(DragStartedEvent e) => Play(AudioType.DragStart);
    private void OnDragEnded(DragEndedEvent e) => Play(AudioType.DragEnd);
    private void OnBlockExited(BlockExitStartedEvent e) => Play(AudioType.BlockExit);
    private void OnLevelCompleted(LevelCompletedEvent e)
        => Play(e.IsWin ? AudioType.LevelWin : AudioType.LevelLose);

    private void Play(AudioType type)
    {
        if (audioSource == null)
        {
            Debug.LogError($"[AudioController] AudioSource bulunamadı!");
            return;
        }
        AudioData data = audioConfig?.Get(type);
        if (data == null)
        {
            Debug.LogWarning($"[AudioController] AudioConfig'te {type} için veri bulunamadı!");
            return;
        }
        AudioClip clip = data.GetRandomClip();
        if (clip == null)
        {
            Debug.LogWarning($"[AudioController] AudioData'da {type} için clip bulunamadı!");
            return;
        }
        if (clip != null) audioSource.PlayOneShot(clip, data.Volume());
    }
}

