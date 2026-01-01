using Application.IntegrationEvent;

namespace Infrastructure.IntegrationEvent;

internal static class RabbitMqIntegrationEventTypeResolver
{
    private static readonly Dictionary<string, Type> Mapping;

    static RabbitMqIntegrationEventTypeResolver()
    {
        Mapping = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .Where(t =>
                typeof(IIntegrationEvent).IsAssignableFrom(t) &&
                !t.IsAbstract)
            .ToDictionary(t => t.Name, t => t);
    }

    public static string GetName(IIntegrationEvent integrationEvent)
    {
        return integrationEvent.GetType().Name;
    }

    public static Type GetType(string name)
    {
        if (!Mapping.TryGetValue(name, out var type))
        {
            throw new TypeLoadException($"Cannot resolve integration event {name}");
        }

        return type;
    }
}
