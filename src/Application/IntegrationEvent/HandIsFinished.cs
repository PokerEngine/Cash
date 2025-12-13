namespace Application.IntegrationEvent;

public record HandIsFinishedIntegrationEvent(
    Guid HandUid,
    Guid TableUid,
    DateTime OccuredAt
) : IIntegrationEvent;

public class HandIsFinishedHandler : IIntegrationEventHandler<HandIsFinishedIntegrationEvent>
{
    public async Task HandleAsync(HandIsFinishedIntegrationEvent integrationEvent)
    {
        await Task.CompletedTask;
    }
}
