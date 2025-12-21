using Application.IntegrationEvent;

namespace Application.Connection;

public interface IConnectionRegistry
{
    void Connect(Guid tableUid, string nickname, IConnection connection);
    void Disconnect(Guid tableUid, string nickname, IConnection connection);
    Task SendIntegrationEventAsync(Guid tableUid, string nickname, IIntegrationEvent integrationEvent);
}
