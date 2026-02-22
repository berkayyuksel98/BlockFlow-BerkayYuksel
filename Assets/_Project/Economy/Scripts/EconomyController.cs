using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

public class EconomyController : MonoBehaviour
{
   private const string SAVE_KEY_PREFIX = "Currency_";

    [SerializeField] private List<Currency> currencies = new List<Currency>();

    //Dependencies
    private EventBus eventBus;

    [Inject]
    public void Construct(EventBus eventBus)
    {
        this.eventBus = eventBus;
    }

    public void Initialize()
    {
        currencies.Add(new Money());

        LoadFromPlayerPrefs();
    }
    
    public void Change(CurrencyType type, long amount)
    {
        var currency = currencies.FirstOrDefault(c => c.Type == type);
        if (currency != null)
        {
            currency.Add(amount);
            SaveToPlayerPrefs(type);
            eventBus.Publish(new ChangeCurrencyEvent(type, amount, currency.GetAmount()));
        }
    }

    public bool CanAfford(CurrencyType type, long amount)
    {
        var currency = currencies.FirstOrDefault(c => c.Type == type);
        return currency != null && currency.CanSpend(amount);
    }

    public long GetAmount(CurrencyType type)
    {
        var currency = currencies.FirstOrDefault(c => c.Type == type);
        return currency != null ? currency.GetAmount() : 0;
    }

    #region  PlayerPrefs Save/Load
    private void SaveToPlayerPrefs(CurrencyType type)
    {
        var currency = currencies.FirstOrDefault(c => c.Type == type);
        if (currency != null)
        {
            string key = SAVE_KEY_PREFIX + type.ToString();
            PlayerPrefs.SetString(key, currency.GetAmount().ToString());
            PlayerPrefs.Save();
        }
    }

    private void LoadFromPlayerPrefs()
    {
        foreach (var currency in currencies)
        {
            string key = SAVE_KEY_PREFIX + currency.Type.ToString();
            if (PlayerPrefs.HasKey(key))
            {
                string savedValue = PlayerPrefs.GetString(key);
                if (long.TryParse(savedValue, out long amount))
                {
                    currency.Set(amount);
                    eventBus.Publish(new ChangeCurrencyEvent(currency.Type, 0, amount));
                }
            }
            else
            {
                // PlayerPrefs'te bu para türü için kayıt bulunamadı, varsayılan değeri kullanıyor {currency.GetAmount()} --- IGNORE ---
            }
        }
    }
    #endregion

    #region Debug Methods
    // Debug: Para ekle
    [ContextMenu("Add 10k Money")]
    private void DebugAddMoney()
    {
        Change(CurrencyType.Money, 10000);
    }

    [ContextMenu("Reset Save Data")]
    private void DebugResetSave()
    {
        foreach (var currency in currencies)
        {
            string key = SAVE_KEY_PREFIX + currency.Type.ToString();
            PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();
        Debug.Log("EconomyController: Save data cleared!");
    }
    #endregion
}
