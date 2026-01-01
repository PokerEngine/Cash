using Application.Event;
using Application.Repository;
using Domain.Entity;

namespace Application.Command;

public record struct StandUpPlayerCommand : ICommand
{
    public required Guid Uid { get; init; }
    public required string Nickname { get; init; }
}

public record struct StandUpPlayerResponse : ICommandResponse
{
    public required Guid Uid { get; init; }
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
            uid: command.Uid,
            events: await repository.GetEventsAsync(command.Uid)
        );

        table.StandUp(command.Nickname);

        var events = table.PullEvents();
        await repository.AddEventsAsync(table.Uid, events);

        var context = new EventContext
        {
            TableUid = table.Uid
        };

        foreach (var @event in events)
        {
            await eventDispatcher.DispatchAsync(@event, context);
        }

        return new StandUpPlayerResponse
        {
            Uid = table.Uid,
            Nickname = command.Nickname
        };
    }
}
