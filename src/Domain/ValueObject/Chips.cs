using Domain.Exception;

namespace Domain.ValueObject;

public readonly struct Chips : IComparable<Chips>, IEquatable<Chips>
{
    private readonly int _amount;

    public Chips(int amount)
    {
        if (amount < 0)
        {
            throw new InsufficientChipsException("Chips amount must be a non-negative integer");
        }

        _amount = amount;
    }

    public static implicit operator int(Chips a)
        => a._amount;

    public static implicit operator Chips(int a)
        => new(a);

    public static Chips operator +(Chips a, Chips b)
        => new(a._amount + b._amount);

    public static Chips operator -(Chips a, Chips b)
    {
        if (a < b)
        {
            throw new InsufficientChipsException("Cannot subtract more chips than available");
        }

        return new(a._amount - b._amount);
    }

    public static Chips operator /(Chips a, int b)
    {
        if (b < 0)
        {
            throw new InsufficientChipsException("Cannot divide by a negative integer");
        }

        return new(a._amount / b);
    }

    public static Chips operator %(Chips a, int b)
    {
        if (b < 0)
        {
            throw new InsufficientChipsException("Cannot divide by a negative integer");
        }

        return new(a._amount % b);
    }

    public static bool operator ==(Chips a, Chips b)
        => a._amount == b._amount;

    public static bool operator !=(Chips a, Chips b)
        => a._amount != b._amount;

    public static bool operator >(Chips a, Chips b)
        => a._amount > b._amount;

    public static bool operator <(Chips a, Chips b)
        => a._amount < b._amount;

    public static bool operator >=(Chips a, Chips b)
        => a._amount >= b._amount;

    public static bool operator <=(Chips a, Chips b)
        => a._amount <= b._amount;

    public int CompareTo(Chips other)
        => _amount.CompareTo(other._amount);

    public bool Equals(Chips other)
        => _amount.Equals(other._amount);

    public override bool Equals(object? o)
        => o is not null && o.GetType() == GetType() && _amount.Equals(((Chips)o)._amount);

    public override int GetHashCode()
        => _amount.GetHashCode();

    public override string ToString()
        => $"{_amount} chip(s)";
}
