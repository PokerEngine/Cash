namespace Application.IntegrationEvent;

public record PlayerSatInIntegrationEvent(
    string NickName,
    int Seat,
    int Stack,
    Guid TableUid,
    DateTime OccuredAt
) : IIntegrationEvent;

public class PlayerSatInHandler : IIntegrationEventHandler<PlayerSatInIntegrationEvent>
{
    public async Task HandleAsync(PlayerSatInIntegrationEvent integrationEvent)
    {
        await Task.CompletedTask;
    }
}
