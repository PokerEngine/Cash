using Application.Connection;

namespace Application.IntegrationEvent;

public struct HoleCardsAreMuckedIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required Guid HandUid { get; init; }
    public required string Nickname { get; init; }
}

public class HoleCardsAreMuckedHandler(
    IConnectionRegistry connectionRegistry
) : IIntegrationEventHandler<HoleCardsAreMuckedIntegrationEvent>
{
    public async Task HandleAsync(HoleCardsAreMuckedIntegrationEvent integrationEvent)
    {
        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );
    }
}
