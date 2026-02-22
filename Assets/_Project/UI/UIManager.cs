using System;
using UnityEngine;
using Zenject;

// Arayüz panellerinin açılıp kapanmasını ve geçişlerini merkezi olarak yöneten sınıf
public class UIManager : IInitializable, IDisposable
{
    private readonly IEventBus eventBus;

    [Inject]
    public UIManager(IEventBus eventBus)
    {
        this.eventBus = eventBus;
    }

    // Başlangıç panelini göster ve EventBus aboneliklerini kur
    public void Initialize()
    {
        eventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
    }

    // Abonelikleri temizler
    public void Dispose()
    {
        eventBus?.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
    }

    private void OnLevelCompleted(LevelCompletedEvent e)
    {
        ShowResultPanel(e.IsWin);
    }

    public void ShowGameplayPanel(int levelIndex)
    {
    }

    public void ShowResultPanel(bool isWin)
    {
    }

    // Tum panelleri gizler
    public void HideAllPanels()
    {
    }


}
