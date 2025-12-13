namespace Application.IntegrationEvent;

public record IntegrationEventParticipant(
    string Nickname,
    int Seat,
    int Stack
);

public record HandIsCreatedIntegrationEvent(
    Guid HandUid,
    string Game,
    int SmallBlind,
    int BigBlind,
    int MaxSeat,
    int SmallBlindSeat,
    int BigBlindSeat,
    int ButtonSeat,
    List<IntegrationEventParticipant> Participants,
    Guid TableUid,
    DateTime OccuredAt
) : IIntegrationEvent;

public class HandIsCreatedHandler : IIntegrationEventHandler<HandIsCreatedIntegrationEvent>
{
    public async Task HandleAsync(HandIsCreatedIntegrationEvent integrationEvent)
    {
        await Task.CompletedTask;
    }
}
