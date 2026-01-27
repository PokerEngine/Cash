using Application.Connection;

namespace Application.IntegrationEvent;

public struct BoardCardsDealtIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required Guid HandUid { get; init; }
    public required string Cards { get; init; }
}

public class BoardCardsDealtHandler(
    IConnectionRegistry connectionRegistry
) : IIntegrationEventHandler<BoardCardsDealtIntegrationEvent>
{
    public async Task HandleAsync(BoardCardsDealtIntegrationEvent integrationEvent)
    {
        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );
    }
}
