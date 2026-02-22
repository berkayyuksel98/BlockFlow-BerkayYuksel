using UnityEngine;

public enum CurrencyType
{
    Money
}
[System.Serializable]
public abstract class Currency
{
    public abstract CurrencyType Type { get; }
    public long Amount { get; protected set; }

    public virtual void Add(long amount) => Amount += amount;

    public virtual void Spend(long amount)
    {
        if (Amount >= amount)
        {
            Amount -= amount;
        }
    }

    public virtual bool TrySpend(long amount)
    {
        if (Amount < amount) return false;
        Amount -= amount;
        return true;
    }

    public virtual bool CanSpend(long amount) => Amount >= amount;
    public virtual bool HasEnough(long amount) => Amount >= amount;

    public virtual long GetAmount() => Amount;
    public virtual void Set(long amount) => Amount = amount;
}


[System.Serializable]
public class Money : Currency
{
    public override CurrencyType Type => CurrencyType.Money;
}
