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
            Game = @event.Rules.Game.ToString(),
            MaxSeat = @event.Rules.MaxSeat,
            SmallBlind = @event.Rules.SmallBlind,
            BigBlind = @event.Rules.BigBlind,
            ChipCostAmount = @event.Rules.ChipCost.Amount,
            ChipCostCurrency = @event.Rules.ChipCost.Currency.ToString(),
            OccurredAt = @event.OccurredAt
        };

        await integrationEventPublisher.PublishAsync(integrationEvent, "cash.table-created");
    }
}
