using Application.Connection;
using Application.IntegrationEvent;
using System.Collections.Concurrent;

namespace Infrastructure.Connection;

public class InMemoryConnectionRegistry(ILogger<InMemoryConnectionRegistry> logger) : IConnectionRegistry
{
    private readonly ConcurrentDictionary<(Guid, string), HashSet<IConnection>> _connections = new();

    public void Connect(Guid tableUid, string nickname, IConnection connection)
    {
        logger.LogInformation("Connect to the table {TableUid} by {Nickname}", tableUid, nickname);

        var key = GetKey(tableUid, nickname);
        var connections = _connections.GetOrAdd(key, _ => new HashSet<IConnection>());
        lock (connections)
            connections.Add(connection);
    }

    public void Disconnect(Guid tableUid, string nickname, IConnection connection)
    {
        logger.LogInformation("Disconnect from the table {TableUid} by {Nickname}", tableUid, nickname);

        var key = GetKey(tableUid, nickname);
        if (_connections.TryGetValue(key, out var connections))
        {
            lock (connections)
                connections.Remove(connection);
        }
    }

    public async Task SendIntegrationEventAsync(Guid tableUid, string nickname, IIntegrationEvent integrationEvent)
    {
        var key = GetKey(tableUid, nickname);
        if (!_connections.TryGetValue(key, out var connections))
            return;

        List<IConnection> snapshot;
        lock (connections)
            snapshot = connections.ToList();

        foreach (var connection in snapshot)
        {
            await connection.SendIntegrationEventAsync(integrationEvent);
        }
    }

    private (Guid, string) GetKey(Guid tableUid, string nickname)
    {
        return (tableUid, nickname);
    }
}
