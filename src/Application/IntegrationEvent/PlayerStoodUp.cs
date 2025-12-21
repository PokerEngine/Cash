using Application.Connection;

namespace Application.IntegrationEvent;

public record struct PlayerStoodUpIntegrationEvent : IIntegrationEvent
{
    public required Guid TableUid { get; init; }
    public required string NickName { get; init; }
    public required DateTime OccuredAt { get; init; }
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
