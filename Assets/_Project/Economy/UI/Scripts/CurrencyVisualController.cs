using DG.Tweening;
using TMPro;
using UnityEngine;
using Zenject;

public class CurrencyVisualController : MonoBehaviour
{
    [SerializeField] private CurrencyType targetCurrencyType;
    private EventBus eventBus;
    [SerializeField] private TextMeshProUGUI currencyText;

    //Dependencies
    private EconomyController economyController;
    private Tween sizeTween;

    [Inject]
    public void Construct(EventBus eventBus,EconomyController economyController)
    {
        this.economyController = economyController;
        this.eventBus = eventBus;
    }

    private void OnEnable()
    {
        SetCurrencyText(economyController.GetAmount(targetCurrencyType));
        eventBus.Subscribe<ChangeCurrencyEvent>(OnChangeCurrency); //eğer bir currency değişikliği olursa UI güncellemek için bağlanıyoruz
    }

    private void OnDisable()
    {
        eventBus.Unsubscribe<ChangeCurrencyEvent>(OnChangeCurrency); // UI güncellemesini durdurmak için aboneliği kaldırıyoruz
    }

    private void SetCurrencyText(long amount)
    {
        currencyText.text = amount.ToString();
    }
    private void OnChangeCurrency(ChangeCurrencyEvent evt)
    {
        if (evt.CurrencyType == targetCurrencyType)
        {
            UpdateCurrencyText(evt.NewAmount);
            PlayChangeAnimation();
        }
    }

    private void UpdateCurrencyText(long newAmount)
    {
        currencyText.text = newAmount.ToString();
    }

    private void PlayChangeAnimation()
    {
        if (sizeTween != null && sizeTween.IsActive())
        {
            return; //animasyon zaten oynuyor tekrar başlatma
        }
        else
        {
            sizeTween = currencyText.transform.DOScale(1.2f, 0.2f).SetLoops(2, LoopType.Yoyo);
        }
    }
}
