using Application.IntegrationEvent;
using Domain.Event;
using Domain.ValueObject;

namespace Application.Event;

public class TableIsCreatedEventHandler(
    IIntegrationEventPublisher integrationEventPublisher
) : IEventHandler<TableIsCreatedEvent>
{
    public async Task HandleAsync(TableIsCreatedEvent @event, TableUid tableUid)
    {
        var integrationEvent = new TableIsCreatedIntegrationEvent
        {
            TableUid = tableUid,
            Game = @event.Game.ToString(),
            MaxSeat = @event.MaxSeat,
            SmallBlind = @event.SmallBlind,
            BigBlind = @event.BigBlind,
            ChipCostAmount = @event.ChipCost.Amount,
            ChipCostCurrency = @event.ChipCost.Currency.ToString(),
            OccuredAt = @event.OccuredAt
        };

        await integrationEventPublisher.PublishAsync(integrationEvent, "cash.table-is-created");
    }
}
