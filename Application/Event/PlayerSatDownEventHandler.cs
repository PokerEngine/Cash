using Application.IntegrationEvent;
using Domain.Event;

namespace Application.Event;

public class PlayerSatDownEventHandler(
    IIntegrationEventPublisher integrationEventPublisher
) : IEventHandler<PlayerSatDownEvent>
{
    public async Task HandleAsync(PlayerSatDownEvent @event, EventContext context)
    {
        var integrationEvent = new PlayerSatDownIntegrationEvent
        {
            Uid = Guid.NewGuid(),
            TableUid = context.TableUid,
            Nickname = @event.Nickname,
            Seat = @event.Seat,
            Stack = @event.Stack,
            OccurredAt = @event.OccurredAt
        };

        await integrationEventPublisher.PublishAsync(integrationEvent, "cash.player-sat-down");
    }
}
