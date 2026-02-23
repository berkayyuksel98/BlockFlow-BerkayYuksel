using DG.Tweening;
using UnityEngine;
using Zenject;

public class ExitView : MonoBehaviour
{
    public ExitData Data { get; private set; }
    [SerializeField] private MeshRenderer meshRenderer;

    [Inject] private GameConfig gameConfig;
    public void Initialize(ExitData data)
    {
        Data = data;
        SetColor(data.Color);
    }

    public void SetColor(BlockColor color)
    {
        if (meshRenderer == null)
        {
            Debug.LogError("[ExitView] MeshRenderer referansı atanmadı!");
            return;
        }
        meshRenderer.material.SetColor("_BaseColor", gameConfig.BlockColors[(int)color]);
    }
    public void PlayExitAnimation(float blockTravelDuration)
    {
        const float dipAmount    = 1f;
        const float dipDuration  = 0.05f;
        const float riseDuration = 0.05f;

        float origY    = transform.position.y;
        float waitTime = blockTravelDuration;

        DOTween.Sequence()
            .Append(transform.DOMoveY(origY - dipAmount, dipDuration).SetEase(Ease.InBack))
            .AppendInterval(waitTime)
            .Append(transform.DOMoveY(origY, riseDuration).SetEase(Ease.OutBack,3f));
    }
}
