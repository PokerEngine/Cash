using Application.Repository;
using Domain.Event;
using Domain.ValueObject;

namespace Application.Test.Stub;

public class StubRepository : IRepository
{
    private readonly Dictionary<TableUid, List<BaseEvent>> _mapping = new();
    private bool _isConnected;

    public async Task ConnectAsync()
    {
        if (_isConnected)
        {
            throw new InvalidOperationException("Already connected");
        }

        _isConnected = true;
        await Task.CompletedTask;
    }

    public async Task DisconnectAsync()
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Not connected");
        }

        _isConnected = false;
        await Task.CompletedTask;
    }

    public async Task<TableUid> GetNextUidAsync()
    {
        await Task.CompletedTask;

        return new TableUid(Guid.NewGuid());
    }

    public async Task<IList<BaseEvent>> GetEventsAsync(TableUid tableUid)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Not connected");
        }

        if (!_mapping.TryGetValue(tableUid, out var events))
        {
            throw new InvalidOperationException("The table is not found");
        }

        await Task.CompletedTask;

        return events;
    }

    public async Task AddEventsAsync(TableUid tableUid, IList<BaseEvent> events)
    {
        if (!_isConnected)
        {
            throw new InvalidOperationException("Not connected");
        }

        if (!_mapping.TryAdd(tableUid, events.ToList()))
        {
            _mapping[tableUid].AddRange(events);
        }

        await Task.CompletedTask;
    }
}
