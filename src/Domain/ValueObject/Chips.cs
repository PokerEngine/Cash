namespace Domain.ValueObject;

public readonly struct Chips : IComparable<Chips>, IEquatable<Chips>
{
    private readonly int _amount;

    public Chips(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Chips amount must be a non-negative integer");
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
        => new(a._amount - b._amount);

    public static Chips operator *(Chips a, int b)
        => new(a._amount * b);

    public static Chips operator /(Chips a, int b)
        => new Chips(a._amount / b);

    public static Chips operator %(Chips a, int b)
        => new(a._amount % b);

    public static bool operator !(Chips a)
        => a._amount == 0;

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

    public static bool operator true(Chips a)
        => a._amount != 0;

    public static bool operator false(Chips a)
        => a._amount == 0;

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
