using Application.Connection;

namespace Application.IntegrationEvent;

public record struct PlayerActionRequestedIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required Guid HandUid { get; init; }
    public required string Nickname { get; init; }
    public required bool FoldIsAvailable { get; init; }
    public required bool CheckIsAvailable { get; init; }
    public required bool CallIsAvailable { get; init; }
    public required int CallByAmount { get; init; }
    public required bool RaiseIsAvailable { get; init; }
    public required int MinRaiseByAmount { get; init; }
    public required int MaxRaiseByAmount { get; init; }
}

public class PlayerActionRequestedHandler(
    IConnectionRegistry connectionRegistry
) : IIntegrationEventHandler<PlayerActionRequestedIntegrationEvent>
{
    public async Task HandleAsync(PlayerActionRequestedIntegrationEvent integrationEvent)
    {
        // TODO: if the player is sitting out, auto-fold instead of requesting a decision

        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );
    }
}
