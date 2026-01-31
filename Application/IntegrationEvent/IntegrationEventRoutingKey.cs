using Application.Exception;
using System.Text.RegularExpressions;

namespace Application.IntegrationEvent;

public readonly struct IntegrationEventRoutingKey
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
            throw new InternalSystemMisconfiguredException("Must start with a latin letter and contain only latin letters, digits, dots, underscores, and dashes");
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
