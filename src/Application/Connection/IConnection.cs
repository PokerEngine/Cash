using Application.IntegrationEvent;

namespace Application.Connection;

public interface IConnection
{
    Task ListenAsync(CancellationToken cancellationToken);
    Task SendIntegrationEventAsync(IIntegrationEvent integrationEvent);
}
