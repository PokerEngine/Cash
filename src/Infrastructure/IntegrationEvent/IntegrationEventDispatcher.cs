using Application.IntegrationEvent;

namespace Infrastructure.IntegrationEvent;

public class IntegrationEventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<IntegrationEventDispatcher> logger
) : IIntegrationEventDispatcher
{
    public async Task DispatchAsync<T>(T integrationEvent) where T : IIntegrationEvent
    {
        logger.LogInformation("Dispatching integration event {IntegrationEventName}", typeof(T).Name);

        var handlerType = typeof(IIntegrationEventHandler<T>);
        var handler = serviceProvider.GetService(handlerType);

        if (handler is null)
        {
            throw new InvalidOperationException("Handler is not found");
        }

        await ((IIntegrationEventHandler<T>)handler).HandleAsync(integrationEvent);
    }
}
