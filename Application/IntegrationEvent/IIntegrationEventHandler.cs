namespace Application.IntegrationEvent;

public interface IIntegrationEventHandler<in T> where T : IIntegrationEvent
{
    Task HandleAsync(T integrationEvent);
}
