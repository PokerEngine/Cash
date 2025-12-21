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
    Task DispatchAsync<T>(T integrationEvent) where T : IIntegrationEvent;
}
