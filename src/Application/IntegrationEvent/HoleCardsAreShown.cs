using Application.Connection;

namespace Application.IntegrationEvent;

public struct HoleCardsAreShownIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required Guid HandUid { get; init; }
    public required string Nickname { get; init; }
    public required string Cards { get; init; }
    public required string Type { get; init; }
    public required int Weight { get; init; }
}

public class HoleCardsAreShownHandler(
    IConnectionRegistry connectionRegistry
) : IIntegrationEventHandler<HoleCardsAreShownIntegrationEvent>
{
    public async Task HandleAsync(HoleCardsAreShownIntegrationEvent integrationEvent)
    {
        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );
    }
}
