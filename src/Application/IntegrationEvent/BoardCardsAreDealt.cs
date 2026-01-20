using Application.Connection;

namespace Application.IntegrationEvent;

public struct BoardCardsAreDealtIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required Guid HandUid { get; init; }
    public required string Cards { get; init; }
}

public class BoardCardsAreDealtHandler(
    IConnectionRegistry connectionRegistry
) : IIntegrationEventHandler<BoardCardsAreDealtIntegrationEvent>
{
    public async Task HandleAsync(BoardCardsAreDealtIntegrationEvent integrationEvent)
    {
        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );
    }
}
