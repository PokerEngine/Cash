using Application.IntegrationEvent;
using Domain.Event;

namespace Application.Event;

public class PlayerStoodUpEventHandler(
    IIntegrationEventPublisher integrationEventPublisher
) : IEventHandler<PlayerStoodUpEvent>
{
    public async Task HandleAsync(PlayerStoodUpEvent @event, EventContext context)
    {
        var integrationEvent = new PlayerStoodUpIntegrationEvent
        {
            Uid = Guid.NewGuid(),
            TableUid = context.TableUid,
            Nickname = @event.Nickname,
            OccurredAt = @event.OccurredAt
        };

        await integrationEventPublisher.PublishAsync(integrationEvent, "cash.player-stood-up");
    }
}
