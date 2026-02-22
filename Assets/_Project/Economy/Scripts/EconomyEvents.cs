using UnityEngine;

public class ChangeCurrencyEvent : IGameEvent
{
    public CurrencyType CurrencyType { get; private set; }
    public long AmountChanged { get; private set; }
    public long NewAmount { get; private set; }

    public ChangeCurrencyEvent(CurrencyType currencyType, long amountChanged, long newAmount)
    {
        CurrencyType = currencyType;
        AmountChanged = amountChanged;
        NewAmount = newAmount;
    }
}
