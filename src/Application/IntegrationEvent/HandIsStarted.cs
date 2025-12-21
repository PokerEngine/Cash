namespace Application.IntegrationEvent;

public record struct HandIsStartedIntegrationEvent : IIntegrationEvent
{
    public required Guid TableUid { get; init; }
    public required Guid HandUid { get; init; }
    public required DateTime OccuredAt { get; init; }
}

public class HandIsStartedHandler : IIntegrationEventHandler<HandIsStartedIntegrationEvent>
{
    public async Task HandleAsync(HandIsStartedIntegrationEvent integrationEvent)
    {
        await Task.CompletedTask;
    }
}
