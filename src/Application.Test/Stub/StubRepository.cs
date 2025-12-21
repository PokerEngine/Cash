using Application.Repository;
using Domain.Event;
using Domain.ValueObject;

namespace Application.Test.Stub;

public class StubRepository : IRepository
{
    private readonly Dictionary<TableUid, List<BaseEvent>> _mapping = new();

    public async Task<TableUid> GetNextUidAsync()
    {
        await Task.CompletedTask;

        return new TableUid(Guid.NewGuid());
    }

    public async Task<List<BaseEvent>> GetEventsAsync(TableUid tableUid)
    {
        if (!_mapping.TryGetValue(tableUid, out var events))
        {
            throw new InvalidOperationException("The table is not found");
        }

        await Task.CompletedTask;

        return events;
    }

    public async Task AddEventsAsync(TableUid tableUid, List<BaseEvent> events)
    {
        if (!_mapping.TryAdd(tableUid, events.ToList()))
        {
            _mapping[tableUid].AddRange(events);
        }

        await Task.CompletedTask;
    }
}
