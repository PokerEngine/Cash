using Application.Repository;
using Domain.Event;
using Domain.ValueObject;
using System.Collections.Concurrent;

namespace Application.Test.Repository;

public class StubRepository : IRepository
{
    private readonly ConcurrentDictionary<TableUid, List<IEvent>> _mapping = new();

    public Task<TableUid> GetNextUidAsync()
    {
        return Task.FromResult(new TableUid(Guid.NewGuid()));
    }

    public Task<List<IEvent>> GetEventsAsync(TableUid tableUid)
    {
        if (!_mapping.TryGetValue(tableUid, out var events))
        {
            throw new InvalidOperationException("The table is not found");
        }

        List<IEvent> snapshot;
        lock (events)
            snapshot = events.ToList();

        return Task.FromResult(snapshot);
    }

    public Task AddEventsAsync(TableUid tableUid, List<IEvent> events)
    {
        var items = _mapping.GetOrAdd(tableUid, _ => new List<IEvent>());
        lock (items)
            items.AddRange(events);

        return Task.CompletedTask;
    }
}
