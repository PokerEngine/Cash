using Application.Repository;
using Domain.Entity;
using Domain.Event;
using Domain.ValueObject;

namespace Application.Command;

public record TableCreateCommand(
    string Game,
    int MaxSeat,
    int SmallBlind,
    int BigBlind,
    decimal ChipCostAmount,
    string ChipCostCurrency
) : ICommand;

public record TableCreateResult(
    Guid TableUid,
    string Game,
    int MaxSeat,
    int SmallBlind,
    int BigBlind,
    decimal ChipCostAmount,
    string ChipCostCurrency
) : IResult;

public class TableCreateHandler(
    IRepository repository
) : ICommandHandler<TableCreateCommand, TableCreateResult>
{
    public async Task<TableCreateResult> HandleAsync(TableCreateCommand command)
    {
        var game = (Game)Enum.Parse(typeof(Game), command.Game);
        var chipCostCurrency = (Currency)Enum.Parse(typeof(Currency), command.ChipCostCurrency);

        var eventBus = new EventBus();
        var events = new List<BaseEvent>();
        var listener = (BaseEvent @event) => events.Add(@event);
        eventBus.Subscribe(listener);

        var table = Table.FromScratch(
            uid: await repository.GetNextUidAsync(),
            game: game,
            maxSeat: command.MaxSeat,
            smallBlind: command.SmallBlind,
            bigBlind: command.BigBlind,
            chipCost: new Money(command.ChipCostAmount, chipCostCurrency),
            eventBus: eventBus
        );

        eventBus.Unsubscribe(listener);

        await repository.AddEventsAsync(table.Uid, events);

        return new TableCreateResult(
            TableUid: table.Uid,
            Game: table.Game.ToString(),
            MaxSeat: table.MaxSeat,
            SmallBlind: table.SmallBlind,
            BigBlind: table.BigBlind,
            ChipCostAmount: table.ChipCost.Amount,
            ChipCostCurrency: table.ChipCost.Currency.ToString()
        );
    }
}
