using Application.Connection;
using Application.IntegrationEvent;
using System.Collections.Concurrent;

namespace Infrastructure.Connection;

public class InMemoryConnectionRegistry(ILogger<InMemoryConnectionRegistry> logger) : IConnectionRegistry
{
    private readonly ConcurrentDictionary<Guid, HashSet<IConnection>> _tableMapping = new();
    private readonly ConcurrentDictionary<(Guid, string), HashSet<IConnection>> _playerMapping = new();

    public void Connect(Guid tableUid, string nickname, IConnection connection)
    {
        logger.LogInformation("Connect {Nickname} to the table {TableUid}", nickname, tableUid);

        var connections = _tableMapping.GetOrAdd(tableUid, _ => new HashSet<IConnection>());
        lock (connections)
            connections.Add(connection);

        connections = _playerMapping.GetOrAdd((tableUid, nickname), _ => new HashSet<IConnection>());
        lock (connections)
            connections.Add(connection);
    }

    public void Disconnect(Guid tableUid, string nickname, IConnection connection)
    {
        logger.LogInformation("Disconnect {Nickname} from the table {TableUid}", nickname, tableUid);

        if (_tableMapping.TryGetValue(tableUid, out var connections))
        {
            lock (connections)
                connections.Remove(connection);
        }

        if (_playerMapping.TryGetValue((tableUid, nickname), out connections))
        {
            lock (connections)
                connections.Remove(connection);
        }
    }

    public async Task SendIntegrationEventToTableAsync(Guid tableUid, IIntegrationEvent integrationEvent)
    {
        if (!_tableMapping.TryGetValue(tableUid, out var connections))
            return;

        List<IConnection> snapshot;
        lock (connections)
            snapshot = connections.ToList();

        foreach (var connection in snapshot)
        {
            await connection.SendIntegrationEventAsync(integrationEvent);
        }
    }

    public async Task SendIntegrationEventToPlayerAsync(
        Guid tableUid,
        string nickname,
        IIntegrationEvent integrationEvent
    )
    {
        if (!_playerMapping.TryGetValue((tableUid, nickname), out var connections))
            return;

        List<IConnection> snapshot;
        lock (connections)
            snapshot = connections.ToList();

        foreach (var connection in snapshot)
        {
            await connection.SendIntegrationEventAsync(integrationEvent);
        }
    }
}
