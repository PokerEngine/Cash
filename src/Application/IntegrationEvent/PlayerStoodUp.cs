namespace Application.IntegrationEvent;

public record PlayerStoodUpIntegrationEvent(
    string NickName,
    Guid TableUid,
    DateTime OccuredAt
) : IIntegrationEvent;

public class PlayerStoodUpHandler : IIntegrationEventHandler<PlayerStoodUpIntegrationEvent>
{
    public async Task HandleAsync(PlayerStoodUpIntegrationEvent integrationEvent)
    {
        await Task.CompletedTask;
    }
}
