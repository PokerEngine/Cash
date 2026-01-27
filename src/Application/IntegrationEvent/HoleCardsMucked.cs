using Application.Connection;

namespace Application.IntegrationEvent;

public struct HoleCardsMuckedIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required Guid HandUid { get; init; }
    public required string Nickname { get; init; }
}

public class HoleCardsMuckedHandler(
    IConnectionRegistry connectionRegistry
) : IIntegrationEventHandler<HoleCardsMuckedIntegrationEvent>
{
    public async Task HandleAsync(HoleCardsMuckedIntegrationEvent integrationEvent)
    {
        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );
    }
}
