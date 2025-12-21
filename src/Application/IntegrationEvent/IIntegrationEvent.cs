namespace Application.IntegrationEvent;

public interface IIntegrationEvent
{
    Guid TableUid { init; get; }
    DateTime OccuredAt { init; get; }
}

public interface IIntegrationEventHandler<in T> where T : IIntegrationEvent
{
    Task HandleAsync(T integrationEvent);
}

public interface IIntegrationEventDispatcher
{
    Task SubscribeAsync<T>(IIntegrationEventHandler<T> handler, IntegrationEventQueue queue) where T : IIntegrationEvent;
    Task UnsubscribeAsync<T>(IIntegrationEventHandler<T> handler, IntegrationEventQueue queue) where T : IIntegrationEvent;
    Task PublishAsync<T>(T integrationEvent, IntegrationEventQueue queue) where T : IIntegrationEvent;
}
