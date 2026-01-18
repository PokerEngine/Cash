using Application.Connection;

namespace Application.IntegrationEvent;

public record struct HandIsFinishedIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required Guid HandUid { get; init; }
}

public class HandIsFinishedHandler(
    IConnectionRegistry connectionRegistry
) : IIntegrationEventHandler<HandIsFinishedIntegrationEvent>
{
    public async Task HandleAsync(HandIsFinishedIntegrationEvent integrationEvent)
    {
        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );
    }
}
