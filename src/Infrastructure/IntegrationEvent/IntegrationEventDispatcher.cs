using Application.IntegrationEvent;

namespace Infrastructure.IntegrationEvent;

public class IntegrationEventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<IntegrationEventDispatcher> logger
)
{
    public async Task DispatchAsync<T>(T integrationEvent) where T : IIntegrationEvent
    {
        logger.LogInformation($"Dispatching integration event {typeof(T).Name}");

        var handlerType = typeof(IIntegrationEventHandler<T>);
        var handler = serviceProvider.GetService(handlerType);

        if (handler is null)
        {
            throw new InvalidOperationException($"No handler found for integration event {typeof(T).Name}");
        }

        await ((IIntegrationEventHandler<T>)handler).HandleAsync(integrationEvent);
    }
}
