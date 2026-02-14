using Application.Repository;
using Application.UnitOfWork;
using Domain.Entity;

namespace Application.Command;

public record SitPlayerDownCommand : ICommand
{
    public required Guid Uid { get; init; }
    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required int Stack { get; init; }
}

public record SitPlayerDownResponse : ICommandResponse
{
    public required Guid Uid { get; init; }
    public required string Nickname { get; init; }
}

public class SitPlayerDownHandler(
    IRepository repository,
    IUnitOfWork unitOfWork
) : ICommandHandler<SitPlayerDownCommand, SitPlayerDownResponse>
{
    public async Task<SitPlayerDownResponse> HandleAsync(SitPlayerDownCommand command)
    {
        var table = Table.FromEvents(
            uid: command.Uid,
            events: await repository.GetEventsAsync(command.Uid)
        );

        table.SitPlayerDown(command.Nickname, command.Seat, command.Stack);

        unitOfWork.RegisterTable(table);
        await unitOfWork.CommitAsync();

        return new SitPlayerDownResponse
        {
            Uid = table.Uid,
            Nickname = command.Nickname,
        };
    }
}
