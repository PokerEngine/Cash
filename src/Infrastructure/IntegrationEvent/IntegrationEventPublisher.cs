using Application.IntegrationEvent;

namespace Infrastructure.IntegrationEvent;

public class IntegrationEventPublisher(
    IIntegrationEventQueue queue
) : IIntegrationEventPublisher
{
    public async Task PublishAsync(
        IIntegrationEvent integrationEvent,
        IntegrationEventChannel channel,
        CancellationToken cancellationToken = default
    )
    {
        await queue.EnqueueAsync(integrationEvent, channel, cancellationToken);
    }
}
