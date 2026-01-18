using Application.Connection;

namespace Application.IntegrationEvent;

public record struct HandIsStartedIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required Guid HandUid { get; init; }
}

public class HandIsStartedHandler(
    IConnectionRegistry connectionRegistry
) : IIntegrationEventHandler<HandIsStartedIntegrationEvent>
{
    public async Task HandleAsync(HandIsStartedIntegrationEvent integrationEvent)
    {
        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );
    }
}
