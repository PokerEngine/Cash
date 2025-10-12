namespace Domain.ValueObject;

public enum Currency
{
    Usd,
    Eur
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
            throw new ArgumentException("Amount must be a non-negative decimal with a 2-digit fraction", nameof(amount));
        }

        Amount = amount;
        Currency = currency;
    }

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InvalidOperationException("Cannot add money with different currencies");
        }

        return new (a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InvalidOperationException("Cannot subtract money with different currencies");
        }

        return new (a.Amount - b.Amount, a.Currency);
    }

    public static bool operator !(Money a)
        => a.Amount == 0;

    public static bool operator ==(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InvalidOperationException("Cannot compare money with different currencies");
        }

        return a.Amount == b.Amount;
    }

    public static bool operator !=(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InvalidOperationException("Cannot compare money with different currencies");
        }

        return a.Amount == b.Amount;
    }

    public static bool operator >(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InvalidOperationException("Cannot compare money with different currencies");
        }

        return a.Amount > b.Amount;
    }

    public static bool operator <(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InvalidOperationException("Cannot compare money with different currencies");
        }

        return a.Amount < b.Amount;
    }

    public static bool operator >=(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InvalidOperationException("Cannot compare money with different currencies");
        }

        return a.Amount >= b.Amount;
    }

    public static bool operator <=(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InvalidOperationException("Cannot compare money with different currencies");
        }

        return a.Amount <= b.Amount;
    }

    public static bool operator true(Money a)
        => a.Amount != 0;

    public static bool operator false(Money a)
        => a.Amount == 0;

    public int CompareTo(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException("Cannot compare money with different currencies");
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
