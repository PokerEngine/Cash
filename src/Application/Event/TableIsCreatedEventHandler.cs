using Application.IntegrationEvent;
using Domain.Event;
using Domain.ValueObject;

namespace Application.Event;

public class TableIsCreatedEventHandler(
    IIntegrationEventPublisher integrationEventPublisher
) : IEventHandler<TableIsCreatedEvent>
{
    public async Task HandleAsync(TableIsCreatedEvent @event, EventContext context)
    {
        var integrationEvent = new TableIsCreatedIntegrationEvent
        {
            TableUid = context.TableUid,
            Game = @event.Game.ToString(),
            MaxSeat = @event.MaxSeat,
            SmallBlind = @event.SmallBlind,
            BigBlind = @event.BigBlind,
            ChipCostAmount = @event.ChipCost.Amount,
            ChipCostCurrency = @event.ChipCost.Currency.ToString(),
            OccurredAt = @event.OccurredAt
        };

        await integrationEventPublisher.PublishAsync(integrationEvent, "cash.table-is-created");
    }
}
