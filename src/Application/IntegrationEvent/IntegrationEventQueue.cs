using System.Text.RegularExpressions;

namespace Application.IntegrationEvent;

public readonly struct IntegrationEventQueue : IEquatable<IntegrationEventQueue>
{
    private readonly string _name;

    private static readonly Regex Pattern = new(
        "^[a-z][a-z0-9_-]*(\\.[a-z][a-z0-9_-]*)*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public IntegrationEventQueue(string name)
    {
        if (!Pattern.IsMatch(name))
        {
            throw new ArgumentException("Must start with a latin letter and contain only latin letters, digits, underscore symbols and dashes", nameof(name));
        }

        _name = name;
    }

    public static implicit operator string(IntegrationEventQueue a)
        => a._name;

    public static implicit operator IntegrationEventQueue(string a)
        => new(a);

    public static bool operator ==(IntegrationEventQueue a, IntegrationEventQueue b)
        => a._name == b._name;

    public static bool operator !=(IntegrationEventQueue a, IntegrationEventQueue b)
        => a._name != b._name;

    public bool Equals(IntegrationEventQueue other)
        => _name.Equals(other._name);

    public override bool Equals(object? o)
    {
        if (o is null || o.GetType() != GetType())
        {
            return false;
        }

        var c = (IntegrationEventQueue)o;
        return _name.Equals(c._name);
    }

    public override int GetHashCode()
        => _name.GetHashCode();

    public override string ToString()
        => _name;
}
