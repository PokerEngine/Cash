namespace Application.IntegrationEvent;

public record struct PlayerSatInIntegrationEvent : IIntegrationEvent
{
    public required Guid TableUid { get; init; }
    public required string NickName { get; init; }
    public required DateTime OccuredAt { get; init; }
}

public class PlayerSatInHandler : IIntegrationEventHandler<PlayerSatInIntegrationEvent>
{
    public async Task HandleAsync(PlayerSatInIntegrationEvent integrationEvent)
    {
        await Task.CompletedTask;
    }
}
