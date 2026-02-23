using DG.Tweening;
using UnityEngine;

// Win ve Lose panellerinin ortak base class'ı
// Show: scale 0 → 1 animasyonu; Hide: anında gizle
public abstract class ResultPanel : MonoBehaviour
{
    private const float AnimDuration = 0.25f;

    public void Show()
    {
        gameObject.SetActive(true);
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, AnimDuration).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void Hide()
    {
        transform.DOKill();
        gameObject.SetActive(false);
    }
}
