namespace Application.IntegrationEvent;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(
        IIntegrationEvent integrationEvent,
        IntegrationEventChannel channel,
        CancellationToken cancellationToken = default
    );
}
