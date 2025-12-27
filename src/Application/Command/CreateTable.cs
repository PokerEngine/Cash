using Application.Event;
using Application.Repository;
using Domain.Entity;
using Domain.ValueObject;

namespace Application.Command;

public record struct CreateTableCommand : ICommand
{
    public required string Game { get; init; }
    public required int MaxSeat { get; init; }
    public required int SmallBlind { get; init; }
    public required int BigBlind { get; init; }
    public required decimal ChipCostAmount { get; init; }
    public required string ChipCostCurrency { get; init; }
}

public record struct CreateTableResponse : ICommandResponse
{
    public required Guid Uid { get; init; }
    public required string Game { get; init; }
    public required int MaxSeat { get; init; }
    public required int SmallBlind { get; init; }
    public required int BigBlind { get; init; }
    public required decimal ChipCostAmount { get; init; }
    public required string ChipCostCurrency { get; init; }
}

public class CreateTableHandler(
    IRepository repository,
    IEventDispatcher eventDispatcher
) : ICommandHandler<CreateTableCommand, CreateTableResponse>
{
    public async Task<CreateTableResponse> HandleAsync(CreateTableCommand command)
    {
        var game = (Game)Enum.Parse(typeof(Game), command.Game);
        var chipCostCurrency = (Currency)Enum.Parse(typeof(Currency), command.ChipCostCurrency);

        var table = Table.FromScratch(
            uid: await repository.GetNextUidAsync(),
            game: game,
            maxSeat: command.MaxSeat,
            smallBlind: command.SmallBlind,
            bigBlind: command.BigBlind,
            chipCost: new Money(command.ChipCostAmount, chipCostCurrency)
        );

        var events = table.PullEvents();
        await repository.AddEventsAsync(table.Uid, events);

        foreach (var @event in events)
        {
            await eventDispatcher.DispatchAsync(@event, table.Uid);
        }

        return new CreateTableResponse
        {
            Uid = table.Uid,
            Game = table.Game.ToString(),
            MaxSeat = table.MaxSeat,
            SmallBlind = table.SmallBlind,
            BigBlind = table.BigBlind,
            ChipCostAmount = table.ChipCost.Amount,
            ChipCostCurrency = table.ChipCost.Currency.ToString()
        };
    }
}
