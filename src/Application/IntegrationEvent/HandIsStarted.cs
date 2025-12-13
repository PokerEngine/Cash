namespace Application.IntegrationEvent;

public record HandIsStartedIntegrationEvent(
    Guid HandUid,
    Guid TableUid,
    DateTime OccuredAt
) : IIntegrationEvent;

public class HandIsStartedHandler : IIntegrationEventHandler<HandIsStartedIntegrationEvent>
{
    public async Task HandleAsync(HandIsStartedIntegrationEvent integrationEvent)
    {
        await Task.CompletedTask;
    }
}
