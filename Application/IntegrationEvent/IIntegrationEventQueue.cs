namespace Application.IntegrationEvent;

public interface IIntegrationEventQueue
{
    Task EnqueueAsync(
        IIntegrationEvent integrationEvent,
        IntegrationEventRoutingKey routingKey,
        CancellationToken cancellationToken = default
    );
    Task<IIntegrationEvent?> DequeueAsync(
        IntegrationEventRoutingKey routingKey,
        CancellationToken cancellationToken = default
    );
}
