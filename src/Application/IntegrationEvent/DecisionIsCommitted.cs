using Application.Connection;

namespace Application.IntegrationEvent;

public record struct DecisionIsCommittedIntegrationEvent : IIntegrationEvent
{
    public required Guid TableUid { get; init; }
    public required Guid HandUid { get; init; }
    public required string DecisionType { get; init; }
    public required int DecisionAmount { get; init; }
    public required DateTime OccurredAt { get; init; }
}

public class DecisionIsCommittedHandler(
    IConnectionRegistry connectionRegistry
) : IIntegrationEventHandler<DecisionIsCommittedIntegrationEvent>
{
    public async Task HandleAsync(DecisionIsCommittedIntegrationEvent integrationEvent)
    {
        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );
    }
}
