namespace Application.IntegrationEvent;

public record struct DecisionIsCommittedIntegrationEvent : IIntegrationEvent
{
    public required Guid TableUid { get; init; }
    public required Guid HandUid { get; init; }
    public required string DecisionType { get; init; }
    public required int DecisionAmount { get; init; }
    public required DateTime OccuredAt { get; init; }
}

public class DecisionIsCommittedHandler : IIntegrationEventHandler<DecisionIsCommittedIntegrationEvent>
{
    public async Task HandleAsync(DecisionIsCommittedIntegrationEvent integrationEvent)
    {
        await Task.CompletedTask;
    }
}
