using Application.Connection;

namespace Application.IntegrationEvent;

public record struct PlayerSatDownIntegrationEvent : IIntegrationEvent
{
    public required Guid TableUid { get; init; }
    public required string NickName { get; init; }
    public required int Seat { get; init; }
    public required int Stack { get; init; }
    public required DateTime OccuredAt { get; init; }
}

public class PlayerSatDownHandler(
    IConnectionRegistry connectionRegistry
) : IIntegrationEventHandler<PlayerSatDownIntegrationEvent>
{
    public async Task HandleAsync(PlayerSatDownIntegrationEvent integrationEvent)
    {
        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );
    }
}
