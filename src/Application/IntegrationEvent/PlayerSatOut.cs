using Application.Connection;

namespace Application.IntegrationEvent;

public record struct PlayerSatOutIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required string Nickname { get; init; }
}

public class PlayerSatOutHandler(
    IConnectionRegistry connectionRegistry
) : IIntegrationEventHandler<PlayerSatOutIntegrationEvent>
{
    public async Task HandleAsync(PlayerSatOutIntegrationEvent integrationEvent)
    {
        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );
    }
}
