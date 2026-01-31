namespace Application.IntegrationEvent;

public interface IIntegrationEventDispatcher
{
    Task DispatchAsync(IIntegrationEvent integrationEvent);
}
