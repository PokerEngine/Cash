namespace Application.IntegrationEvent;

public record struct HandIsFinishedIntegrationEvent : IIntegrationEvent
{
    public required Guid TableUid { get; init; }
    public required Guid HandUid { get; init; }
    public required DateTime OccuredAt { get; init; }
}

public class HandIsFinishedHandler : IIntegrationEventHandler<HandIsFinishedIntegrationEvent>
{
    public async Task HandleAsync(HandIsFinishedIntegrationEvent integrationEvent)
    {
        await Task.CompletedTask;
    }
}
