using Application.Repository;
using Application.UnitOfWork;
using Domain.Entity;

namespace Application.Command;

public record StandPlayerUpCommand : ICommand
{
    public required Guid Uid { get; init; }
    public required string Nickname { get; init; }
}

public record StandPlayerUpResponse : ICommandResponse
{
    public required Guid Uid { get; init; }
    public required string Nickname { get; init; }
}

public class StandPlayerUpHandler(
    IRepository repository,
    IUnitOfWork unitOfWork
) : ICommandHandler<StandPlayerUpCommand, StandPlayerUpResponse>
{
    public async Task<StandPlayerUpResponse> HandleAsync(StandPlayerUpCommand command)
    {
        var table = Table.FromEvents(
            uid: command.Uid,
            events: await repository.GetEventsAsync(command.Uid)
        );

        table.StandPlayerUp(command.Nickname);

        unitOfWork.RegisterTable(table);
        await unitOfWork.CommitAsync();

        return new StandPlayerUpResponse
        {
            Uid = table.Uid,
            Nickname = command.Nickname
        };
    }
}
