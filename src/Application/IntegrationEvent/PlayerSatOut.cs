namespace Application.IntegrationEvent;

public record PlayerSatOutIntegrationEvent(
    string NickName,
    int Seat,
    int Stack,
    Guid TableUid,
    DateTime OccuredAt
) : IIntegrationEvent;

public class PlayerSatOutHandler : IIntegrationEventHandler<PlayerSatOutIntegrationEvent>
{
    public async Task HandleAsync(PlayerSatOutIntegrationEvent integrationEvent)
    {
        await Task.CompletedTask;
    }
}
