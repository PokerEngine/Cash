using Application.Repository;
using Domain.Event;
using Domain.ValueObject;

namespace Infrastructure.Repository;

public class InMemoryRepository(ILogger<InMemoryRepository> logger) : IRepository
{
    private readonly Dictionary<TableUid, List<BaseEvent>> _mapping = new();

    public async Task<TableUid> GetNextUidAsync()
    {
        await Task.CompletedTask;

        return new TableUid(Guid.NewGuid());
    }

    public async Task<IList<BaseEvent>> GetEventsAsync(TableUid tableUid)
    {
        if (!_mapping.TryGetValue(tableUid, out var events))
        {
            throw new InvalidOperationException("The table is not found");
        }

        await Task.CompletedTask;

        logger.LogInformation("{eventCount} events are got for {tableUid}", events.Count, tableUid);
        return events;
    }

    public async Task AddEventsAsync(TableUid tableUid, IList<BaseEvent> events)
    {
        if (!_mapping.TryAdd(tableUid, events.ToList()))
        {
            _mapping[tableUid].AddRange(events);
        }

        await Task.CompletedTask;

        logger.LogInformation("{eventCount} events are added for {tableUid}", events.Count, tableUid);
    }
}
