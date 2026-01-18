using Application.Connection;

namespace Application.IntegrationEvent;

public record struct PlayerSatInIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required string Nickname { get; init; }
}

public class PlayerSatInHandler(
    IConnectionRegistry connectionRegistry
) : IIntegrationEventHandler<PlayerSatInIntegrationEvent>
{
    public async Task HandleAsync(PlayerSatInIntegrationEvent integrationEvent)
    {
        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );
    }
}
