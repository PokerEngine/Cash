namespace Domain.ValueObject;

public readonly struct Nickname : IEquatable<Nickname>
{
    private readonly string _name;

    public Nickname(string name)
    {
        _name = name;
    }

    public static implicit operator string(Nickname a)
        => a._name;

    public static implicit operator Nickname(string a)
        => new(a);

    public static bool operator ==(Nickname a, Nickname b)
        => a._name == b._name;

    public static bool operator !=(Nickname a, Nickname b)
        => a._name != b._name;

    public bool Equals(Nickname other)
        => _name.Equals(other._name);

    public override bool Equals(object? o)
        => o is not null && o.GetType() == GetType() && _name.Equals(((Nickname)o)._name);

    public override int GetHashCode()
        => _name.GetHashCode();

    public override string ToString()
        => _name;
}
