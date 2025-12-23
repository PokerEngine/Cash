using Application.Repository;
using Domain.Entity;

namespace Application.Command;

public record struct StandUpPlayerCommand : ICommandRequest
{
    public required Guid TableUid { get; init; }
    public required string Nickname { get; init; }
}

public record struct StandUpPlayerResponse : ICommandResponse
{
    public required Guid TableUid { get; init; }
    public required string Nickname { get; init; }
}

public class StandUpPlayerHandler(
    IRepository repository
) : ICommandHandler<StandUpPlayerCommand, StandUpPlayerResponse>
{
    public async Task<StandUpPlayerResponse> HandleAsync(StandUpPlayerCommand command)
    {
        var table = Table.FromEvents(
            uid: command.TableUid,
            events: await repository.GetEventsAsync(command.TableUid)
        );

        table.StandUp(command.Nickname);

        var events = table.PullEvents();
        await repository.AddEventsAsync(table.Uid, events);

        return new StandUpPlayerResponse
        {
            TableUid = table.Uid,
            Nickname = command.Nickname
        };
    }
}
