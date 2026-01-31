using Application.Exception;
using Domain.Event;

namespace Infrastructure.Repository;

internal static class MongoDbEventTypeResolver
{
    private static readonly Dictionary<string, Type> Mapping;

    static MongoDbEventTypeResolver()
    {
        Mapping = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .Where(t =>
                typeof(IEvent).IsAssignableFrom(t) &&
                !t.IsAbstract)
            .ToDictionary(t => t.Name, t => t);
    }

    public static string GetName(IEvent integrationEvent)
    {
        return integrationEvent.GetType().Name;
    }

    public static Type GetType(string name)
    {
        if (!Mapping.TryGetValue(name, out var type))
        {
            throw new InternalSystemMisconfiguredException($"Cannot resolve event {name}");
        }

        return type;
    }
}
