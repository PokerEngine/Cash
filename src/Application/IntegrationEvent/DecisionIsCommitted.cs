namespace Application.IntegrationEvent;

public record DecisionIsCommittedIntegrationEvent(
    Guid HandUid,
    Guid TableUid,
    string DecisionType,
    int DecisionAmount,
    DateTime OccuredAt
) : IIntegrationEvent;

public class DecisionIsCommittedHandler : IIntegrationEventHandler<DecisionIsCommittedIntegrationEvent>
{
    public async Task HandleAsync(DecisionIsCommittedIntegrationEvent integrationEvent)
    {
        await Task.CompletedTask;
    }
}
