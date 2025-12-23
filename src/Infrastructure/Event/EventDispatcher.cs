using Application.Event;
using Domain.Event;
using Domain.ValueObject;

namespace Infrastructure.Event;

public class EventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<EventDispatcher> logger
) : IEventDispatcher
{
    public async Task DispatchAsync<T>(T @event, TableUid tableUid) where T : IEvent
    {
        logger.LogInformation("Dispatching event {EventName} of table {TableUid}", typeof(T).Name, tableUid);

        var handlerType = typeof(IEventHandler<T>);
        var handler = serviceProvider.GetService(handlerType);

        if (handler is null)
        {
            // It's a regular case when we don't handle all events
            return;
        }

        await ((IEventHandler<T>)handler).HandleAsync(@event, tableUid);
    }
}
