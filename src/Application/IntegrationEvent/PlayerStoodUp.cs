using Application.Connection;

namespace Application.IntegrationEvent;

public record struct PlayerStoodUpIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required string Nickname { get; init; }
}

public class PlayerStoodUpHandler(
    IConnectionRegistry connectionRegistry
) : IIntegrationEventHandler<PlayerStoodUpIntegrationEvent>
{
    public async Task HandleAsync(PlayerStoodUpIntegrationEvent integrationEvent)
    {
        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );
    }
}
