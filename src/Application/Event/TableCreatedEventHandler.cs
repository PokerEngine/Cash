using Application.IntegrationEvent;
using Domain.Event;

namespace Application.Event;

public class TableCreatedEventHandler(
    IIntegrationEventPublisher integrationEventPublisher
) : IEventHandler<TableCreatedEvent>
{
    public async Task HandleAsync(TableCreatedEvent @event, EventContext context)
    {
        var integrationEvent = new TableCreatedIntegrationEvent
        {
            Uid = Guid.NewGuid(),
            TableUid = context.TableUid,
            Game = @event.Game.ToString(),
            MaxSeat = @event.MaxSeat,
            SmallBlind = @event.SmallBlind,
            BigBlind = @event.BigBlind,
            ChipCostAmount = @event.ChipCost.Amount,
            ChipCostCurrency = @event.ChipCost.Currency.ToString(),
            OccurredAt = @event.OccurredAt
        };

        await integrationEventPublisher.PublishAsync(integrationEvent, "cash.table-created");
    }
}
