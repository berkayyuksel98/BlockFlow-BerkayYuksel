using TMPro;
using UnityEngine;
using Zenject;

// Oyun içi HUD: level numarası ve geri sayım sayacını gösterir
public class GameplayPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI timerText;

    private ILevelManager levelManager;

    [Inject]
    private void Construct(ILevelManager levelManager)
    {
        this.levelManager = levelManager;
    }

    private void Update()
    {
        if (!gameObject.activeSelf || levelManager == null) return;
        float t = levelManager.GetRemainingTime();
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void Show(int levelIndex)
    {
        levelText.text = $"Level {levelIndex + 1}";
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);
}
