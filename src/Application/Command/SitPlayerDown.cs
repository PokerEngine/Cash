using Application.Event;
using Application.Repository;
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
    IEventDispatcher eventDispatcher
) : ICommandHandler<SitPlayerDownCommand, SitPlayerDownResponse>
{
    public async Task<SitPlayerDownResponse> HandleAsync(SitPlayerDownCommand command)
    {
        var table = Table.FromEvents(
            uid: command.Uid,
            events: await repository.GetEventsAsync(command.Uid)
        );

        table.SitPlayerDown(command.Nickname, command.Seat, command.Stack);

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

        return new SitPlayerDownResponse
        {
            Uid = table.Uid,
            Nickname = command.Nickname,
        };
    }
}
