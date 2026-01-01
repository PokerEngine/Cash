using Application.IntegrationEvent;
using Domain.Event;
using Domain.ValueObject;

namespace Application.Event;

public class PlayerSatDownEventHandler(
    IIntegrationEventPublisher integrationEventPublisher
) : IEventHandler<PlayerSatDownEvent>
{
    public async Task HandleAsync(PlayerSatDownEvent @event, EventContext context)
    {
        var integrationEvent = new PlayerSatDownIntegrationEvent
        {
            TableUid = context.TableUid,
            NickName = @event.Nickname,
            Seat = @event.Seat,
            Stack = @event.Stack,
            OccuredAt = @event.OccuredAt
        };

        await integrationEventPublisher.PublishAsync(integrationEvent, "cash.player-sat-down");
    }
}
