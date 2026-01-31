namespace Domain.ValueObject;

public readonly struct HandUid : IEquatable<HandUid>
{
    private readonly Guid _guid;

    public HandUid(Guid guid)
    {
        _guid = guid;
    }

    public static implicit operator Guid(HandUid a)
        => a._guid;

    public static implicit operator HandUid(Guid a)
        => new(a);

    public static bool operator ==(HandUid a, HandUid b)
        => a._guid == b._guid;

    public static bool operator !=(HandUid a, HandUid b)
        => a._guid != b._guid;

    public bool Equals(HandUid other)
        => _guid.Equals(other._guid);

    public override bool Equals(object? o)
        => o is not null && o.GetType() == GetType() && _guid.Equals(((HandUid)o)._guid);

    public override string ToString()
        => _guid.ToString();

    public override int GetHashCode()
        => _guid.GetHashCode();
}
