namespace Domain.ValueObject;

public readonly struct TableUid : IEquatable<TableUid>
{
    private readonly Guid _guid;

    public TableUid(Guid guid)
    {
        _guid = guid;
    }

    public static implicit operator Guid(TableUid a)
        => a._guid;

    public static implicit operator TableUid(Guid a)
        => new(a);

    public static bool operator ==(TableUid a, TableUid b)
        => a._guid == b._guid;

    public static bool operator !=(TableUid a, TableUid b)
        => a._guid != b._guid;

    public bool Equals(TableUid other)
        => _guid.Equals(other._guid);

    public override bool Equals(object? o)
        => o is not null && o.GetType() == GetType() && _guid.Equals(((TableUid)o)._guid);

    public override string ToString()
        => _guid.ToString();

    public override int GetHashCode()
        => _guid.GetHashCode();
}
