using Application.IntegrationEvent;
using Domain.Event;
using Domain.ValueObject;

namespace Application.Event;

public class PlayerStoodUpEventHandler(
    IIntegrationEventPublisher integrationEventPublisher
) : IEventHandler<PlayerStoodUpEvent>
{
    public async Task HandleAsync(PlayerStoodUpEvent @event, EventContext context)
    {
        var integrationEvent = new PlayerStoodUpIntegrationEvent
        {
            TableUid = context.TableUid,
            Nickname = @event.Nickname,
            OccuredAt = @event.OccuredAt
        };

        await integrationEventPublisher.PublishAsync(integrationEvent, "cash.player-stood-up");
    }
}
