#if UNITY_EDITOR
using UnityEngine;
using Zenject;

public class MockLevelManager : IInitializable, ILevelManager
{
    public LevelData CurrentLevel => null;

    public int CurrentLevelIndex => 0;

    public float GetRemainingTime()
    {
        return 0;
    }

    public void Initialize()
    {
    }

    public void LoadLevel(int index)
    {
    }

    public void LoadNextLevel()
    {
    }

    public void ReloadCurrentLevel()
    {
    }
}
#endif
