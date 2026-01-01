using Application.Event;
using Domain.Event;
using Domain.ValueObject;
using System.Collections.Concurrent;

namespace Application.Test.Event;

public class StubEventDispatcher : IEventDispatcher
{
    private readonly ConcurrentDictionary<TableUid, List<IEvent>> _mapping = new();

    public Task DispatchAsync(IEvent @event, EventContext context)
    {
        var items = _mapping.GetOrAdd(context.TableUid, _ => new List<IEvent>());
        lock (items)
            items.Add(@event);

        return Task.CompletedTask;
    }

    public Task<List<IEvent>> GetDispatchedEvents(TableUid tableUid)
    {
        if (!_mapping.TryGetValue(tableUid, out var events))
        {
            return Task.FromResult(new List<IEvent>());
        }

        List<IEvent> snapshot;
        lock (events)
            snapshot = events.ToList();

        return Task.FromResult(snapshot);
    }

    public Task ClearDispatchedEvents(TableUid tableUid)
    {
        if (_mapping.TryGetValue(tableUid, out var items))
        {
            lock (items)
            {
                items.Clear();
            }
        }

        return Task.CompletedTask;
    }
}
