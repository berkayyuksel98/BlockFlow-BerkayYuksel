public interface ILevelManager
{
    // O an aktif olan level verisi
    LevelData CurrentLevel { get; }

    // PlayerPrefs'te saklanan level indeksi
    int CurrentLevelIndex { get; }

    // Belirtilen indeksteki level'ı yükle
    void LoadLevel(int index);

    // Bir sonraki level'a geç
    void LoadNextLevel();

    // Mevcut level'ı baştan yükle (restart)
    void ReloadCurrentLevel();

    // UI için anlık kalan süre (saniye)
    float GetRemainingTime();
}