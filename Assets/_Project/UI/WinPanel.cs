using UnityEngine.UI;
using Zenject;

// Kazanma ekranı: "Next Level" butonu ile bir sonraki levela geçer
public class WinPanel : ResultPanel
{
    [UnityEngine.SerializeField] private Button nextLevelButton;

    [Inject]
    private void Construct(GameManager gameManager)
    {
        nextLevelButton.onClick.AddListener(() => { gameManager.GoToNextLevel(); Hide(); });
    }
}
