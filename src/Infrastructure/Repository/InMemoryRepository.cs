using Application.Repository;
using Domain.Event;
using Domain.ValueObject;
using System.Collections.Concurrent;

namespace Infrastructure.Repository;

public class InMemoryRepository(ILogger<InMemoryRepository> logger) : IRepository
{
    private readonly ConcurrentDictionary<TableUid, List<BaseEvent>> _mapping = new();

    public async Task<TableUid> GetNextUidAsync()
    {
        await Task.CompletedTask;

        return new TableUid(Guid.NewGuid());
    }

    public Task<List<BaseEvent>> GetEventsAsync(TableUid tableUid)
    {
        if (!_mapping.TryGetValue(tableUid, out var events))
        {
            throw new InvalidOperationException("The table is not found");
        }

        List<BaseEvent> snapshot;
        lock (events)
            snapshot = events.ToList();

        logger.LogInformation("{eventCount} events are got for {tableUid}", snapshot.Count, tableUid);
        return Task.FromResult(snapshot);
    }

    public Task AddEventsAsync(TableUid tableUid, List<BaseEvent> events)
    {
        var items = _mapping.GetOrAdd(tableUid, _ => new List<BaseEvent>());
        lock (items)
            items.AddRange(events);

        logger.LogInformation("{eventCount} events are added for {tableUid}", events.Count, tableUid);
        return Task.CompletedTask;
    }
}
