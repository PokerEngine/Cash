using Application.Event;
using Application.Repository;
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
    IEventDispatcher eventDispatcher
) : ICommandHandler<StandPlayerUpCommand, StandPlayerUpResponse>
{
    public async Task<StandPlayerUpResponse> HandleAsync(StandPlayerUpCommand command)
    {
        var table = Table.FromEvents(
            uid: command.Uid,
            events: await repository.GetEventsAsync(command.Uid)
        );

        table.StandPlayerUp(command.Nickname);

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

        return new StandPlayerUpResponse
        {
            Uid = table.Uid,
            Nickname = command.Nickname
        };
    }
}
