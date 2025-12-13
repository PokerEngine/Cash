namespace Application.IntegrationEvent;

public record PlayerSatDownIntegrationEvent(
    string NickName,
    int Seat,
    int Stack,
    Guid TableUid,
    DateTime OccuredAt
) : IIntegrationEvent;

public class PlayerSatDownHandler : IIntegrationEventHandler<PlayerSatDownIntegrationEvent>
{
    public async Task HandleAsync(PlayerSatDownIntegrationEvent integrationEvent)
    {
        await Task.CompletedTask;
    }
}
