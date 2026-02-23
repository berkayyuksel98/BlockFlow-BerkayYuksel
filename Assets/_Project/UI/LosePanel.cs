using UnityEngine.UI;
using Zenject;

// Kaybetme ekranı: "Restart" butonu ile mevcut leveli yeniden başlatır
public class LosePanel : ResultPanel
{
    [UnityEngine.SerializeField] private Button restartButton;

    [Inject]
    private void Construct(GameManager gameManager)
    {
        restartButton.onClick.AddListener(() => { gameManager.RestartLevel(); Hide(); });
    }
}
