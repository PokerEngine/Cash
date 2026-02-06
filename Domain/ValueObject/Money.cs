using Domain.Exception;

namespace Domain.ValueObject;

public enum Currency
{
    Usd
}

public readonly struct Money : IEquatable<Money>, IComparable<Money>
{
    public readonly decimal Amount;
    public readonly Currency Currency;
    public bool IsZero => Amount == 0;

    public Money(decimal amount, Currency currency)
    {
        if (amount < 0 || decimal.Round(amount, 2) != amount)
        {
            throw new InsufficientMoneyException("Amount must be a non-negative decimal with a 2-digit fraction");
        }

        Amount = amount;
        Currency = currency;
    }

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InsufficientMoneyException("Cannot add money with different currencies");
        }

        return new(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InsufficientMoneyException("Cannot subtract money with different currencies");
        }

        if (a.Amount < b.Amount)
        {
            throw new InsufficientMoneyException("Cannot subtract more money than available");
        }

        return new(a.Amount - b.Amount, a.Currency);
    }

    public static Money operator *(Money a, Chips b)
    {
        return new(a.Amount * b, a.Currency);
    }

    public static Money operator *(Chips a, Money b)
    {
        return new(b.Amount * a, b.Currency);
    }

    public static bool operator ==(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InsufficientMoneyException("Cannot compare money with different currencies");
        }

        return a.Amount == b.Amount;
    }

    public static bool operator !=(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InsufficientMoneyException("Cannot compare money with different currencies");
        }

        return a.Amount == b.Amount;
    }

    public static bool operator >(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InsufficientMoneyException("Cannot compare money with different currencies");
        }

        return a.Amount > b.Amount;
    }

    public static bool operator <(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InsufficientMoneyException("Cannot compare money with different currencies");
        }

        return a.Amount < b.Amount;
    }

    public static bool operator >=(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InsufficientMoneyException("Cannot compare money with different currencies");
        }

        return a.Amount >= b.Amount;
    }

    public static bool operator <=(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InsufficientMoneyException("Cannot compare money with different currencies");
        }

        return a.Amount <= b.Amount;
    }

    public int CompareTo(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InsufficientMoneyException("Cannot compare money with different currencies");
        }

        return Amount.CompareTo(other.Amount);
    }

    public bool Equals(Money other)
        => Amount.Equals(other.Amount) && Currency.Equals(other.Currency);

    public override bool Equals(object? o)
        => o is not null && o.GetType() == GetType() && Equals(o);

    public override int GetHashCode()
        => HashCode.Combine(Amount, Currency);

    public override string ToString() =>
        $"{Amount:0.##} {Currency}";
}
