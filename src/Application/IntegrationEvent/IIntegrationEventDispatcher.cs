namespace Application.IntegrationEvent;

public interface IIntegrationEventDispatcher
{
    Task DispatchAsync<T>(T integrationEvent) where T : IIntegrationEvent;
}
