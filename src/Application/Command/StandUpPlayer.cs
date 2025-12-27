using Application.Event;
using Application.Repository;
using Domain.Entity;

namespace Application.Command;

public record struct StandUpPlayerCommand : ICommand
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
    IRepository repository,
    IEventDispatcher eventDispatcher
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

        foreach (var @event in events)
        {
            await eventDispatcher.DispatchAsync(@event, table.Uid);
        }

        return new StandUpPlayerResponse
        {
            TableUid = table.Uid,
            Nickname = command.Nickname
        };
    }
}
