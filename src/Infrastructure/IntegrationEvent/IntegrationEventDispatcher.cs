using Application.IntegrationEvent;

namespace Infrastructure.IntegrationEvent;

public class IntegrationEventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<IntegrationEventDispatcher> logger
) : IIntegrationEventDispatcher
{
    public async Task DispatchAsync(IIntegrationEvent integrationEvent)
    {
        logger.LogInformation("Dispatching {IntegrationEvent}", integrationEvent);

        var integrationEventType = integrationEvent.GetType();
        var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(integrationEventType);
        var handler = serviceProvider.GetService(handlerType);

        if (handler is null)
        {
            logger.LogDebug("No handler is found for {IntegrationEventName}", integrationEventType.Name);
            // It's a regular case when we don't handle all events
            return;
        }

        var method = handlerType.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))!;

        await (Task)method.Invoke(
            handler,
            new object[] { integrationEvent }
        )!;
    }
}
