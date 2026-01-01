namespace Application.IntegrationEvent;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(
        IIntegrationEvent integrationEvent,
        IntegrationEventRoutingKey routingKey,
        CancellationToken cancellationToken = default
    );
}
