using Application.Event;
using Domain.Event;
using Domain.ValueObject;

namespace Infrastructure.Event;

public class EventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<EventDispatcher> logger
) : IEventDispatcher
{
    public async Task DispatchAsync(IEvent @event, EventContext context)
    {
        var eventType = @event.GetType();

        logger.LogInformation("Dispatching {Event} in {Context}", @event, context);

        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
        var handler = serviceProvider.GetService(handlerType);

        if (handler is null)
        {
            logger.LogDebug("No handler is found for {EventName}", eventType.Name);
            // It's a regular case when we don't handle all events
            return;
        }

        var method = handlerType.GetMethod(nameof(IEventHandler<IEvent>.HandleAsync))!;

        await (Task)method.Invoke(
            handler,
            new object[] { @event, context }
        )!;
    }
}
