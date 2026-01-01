using System.Text.RegularExpressions;

namespace Application.IntegrationEvent;

public readonly record struct IntegrationEventRoutingKey
{
    private static readonly Regex Pattern = new(
        "^[a-z][a-z0-9_-]*(\\.[a-z][a-z0-9_-]*)*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );
    private readonly string _name;

    public IntegrationEventRoutingKey(string name)
    {
        if (!Pattern.IsMatch(name))
        {
            throw new ArgumentException("Must start with a latin letter and contain only latin letters, digits, dots, underscores, and dashes", nameof(name));
        }

        _name = name;
    }

    public static implicit operator string(IntegrationEventRoutingKey a)
        => a._name;

    public static implicit operator IntegrationEventRoutingKey(string a)
        => new(a);

    public override string ToString()
        => _name;
}
