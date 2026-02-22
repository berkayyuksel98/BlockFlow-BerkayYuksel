using UnityEngine;

// BlockFlow oyununa ait tüm event yapıları bu dosyada toplanir
// Struct kullanılır cunku sadece veri tasirlar heap allocation yapmazlar

// Level tamamlandiginda kazanma veya kaybetme durumunda yayınlanan event
public struct LevelCompletedEvent : IGameEvent
{
    public int LevelIndex;
    public bool IsWin;
    public float RemainingTime;
}


// Geri sayım süresi sona erdiğinde LevelManager tarafından yayınlanan event
public struct TimerExpiredEvent : IGameEvent { }
