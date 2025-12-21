namespace Application.IntegrationEvent;

public record struct PlayerSatOutIntegrationEvent : IIntegrationEvent
{
    public required Guid TableUid { get; init; }
    public required string NickName { get; init; }
    public required DateTime OccuredAt { get; init; }
}

public class PlayerSatOutHandler : IIntegrationEventHandler<PlayerSatOutIntegrationEvent>
{
    public async Task HandleAsync(PlayerSatOutIntegrationEvent integrationEvent)
    {
        await Task.CompletedTask;
    }
}
