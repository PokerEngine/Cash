using Application.Event;
using Application.Repository;
using Application.Storage;
using Domain.Entity;

namespace Application.UnitOfWork;

public class UnitOfWork(
    IRepository repository,
    IStorage storage,
    IEventDispatcher eventDispatcher
) : IUnitOfWork
{
    private readonly HashSet<Table> _tables = [];

    public void RegisterTable(Table table)
    {
        _tables.Add(table);
    }

    public async Task CommitAsync(bool updateViews = true)
    {
        foreach (var table in _tables)
        {
            var events = table.PullEvents();

            if (events.Count == 0)
            {
                continue;
            }

            await repository.AddEventsAsync(table.Uid, events);

            if (updateViews)
            {
                await storage.SaveViewAsync(table);
            }

            var context = new EventContext { TableUid = table.Uid };
            foreach (var @event in events)
            {
                await eventDispatcher.DispatchAsync(@event, context);
            }
        }

        _tables.Clear();
    }
}
