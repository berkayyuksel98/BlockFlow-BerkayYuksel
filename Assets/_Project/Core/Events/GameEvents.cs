using UnityEngine;

public struct LevelLoadedEvent : IGameEvent
{
    public int LevelIndex;
}

// Grid boyutu belli olur olmaz yayınlanır; kamera bu event ile konumlanır
public struct GridBuiltEvent : IGameEvent
{
    public int Columns;
    public int Rows;
}

public struct LevelCompletedEvent : IGameEvent
{
    public int LevelIndex;
    public bool IsWin;
    public float RemainingTime;
}

public struct TimerExpiredEvent : IGameEvent { }

public struct DragStartedEvent : IGameEvent { }
public struct DragEndedEvent : IGameEvent { }

public struct BlockExitedEvent : IGameEvent //blok tamamen çıkış noktasından çıktıktan sonra yayınlanır
{
    public BlockColor Color;
}

// Blok çıkış animasyonu başlarken yayınlanır; VFX sistemi buna subscribe olur
public struct BlockExitStartedEvent : IGameEvent
{
    public BlockColor Color;
    public Vector3 ExitWorldPosition;
    public Vector3 ExitDirection;
    public int ExitSize;
    public float Duration;
}
