using Application.Repository;
using Application.UnitOfWork;
using Domain.Entity;
using Domain.ValueObject;

namespace Application.Command;

public record CreateTableCommand : ICommand
{
    public required CreateTableCommandRules Rules { get; init; }
}

public record CreateTableCommandRules : ICommand
{
    public required string Game { get; init; }
    public required int MaxSeat { get; init; }
    public required int SmallBlind { get; init; }
    public required int BigBlind { get; init; }
    public required decimal ChipCostAmount { get; init; }
    public required string ChipCostCurrency { get; init; }
}

public record CreateTableResponse : ICommandResponse
{
    public required Guid Uid { get; init; }
}

public class CreateTableHandler(
    IRepository repository,
    IUnitOfWork unitOfWork
) : ICommandHandler<CreateTableCommand, CreateTableResponse>
{
    public async Task<CreateTableResponse> HandleAsync(CreateTableCommand command)
    {
        var game = (Game)Enum.Parse(typeof(Game), command.Rules.Game);
        var chipCostCurrency = (Currency)Enum.Parse(typeof(Currency), command.Rules.ChipCostCurrency);

        var table = Table.FromScratch(
            uid: await repository.GetNextUidAsync(),
            rules: new Rules
            {
                Game = game,
                MaxSeat = command.Rules.MaxSeat,
                SmallBlind = command.Rules.SmallBlind,
                BigBlind = command.Rules.BigBlind,
                ChipCost = new Money(command.Rules.ChipCostAmount, chipCostCurrency)
            }
        );

        unitOfWork.RegisterTable(table);
        await unitOfWork.CommitAsync();

        return new CreateTableResponse
        {
            Uid = table.Uid
        };
    }
}
