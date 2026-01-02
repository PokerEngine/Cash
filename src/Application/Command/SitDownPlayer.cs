using Application.Event;
using Application.Repository;
using Domain.Entity;

namespace Application.Command;

public record struct SitDownPlayerCommand : ICommand
{
    public required Guid Uid { get; init; }
    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required int Stack { get; init; }
}

public record struct SitDownPlayerResponse : ICommandResponse
{
    public required Guid Uid { get; init; }
    public required string Nickname { get; init; }
}

public class SitDownPlayerHandler(
    IRepository repository,
    IEventDispatcher eventDispatcher
) : ICommandHandler<SitDownPlayerCommand, SitDownPlayerResponse>
{
    public async Task<SitDownPlayerResponse> HandleAsync(SitDownPlayerCommand command)
    {
        var table = Table.FromEvents(
            uid: command.Uid,
            events: await repository.GetEventsAsync(command.Uid)
        );

        table.SitDown(command.Nickname, command.Seat, command.Stack);

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

        return new SitDownPlayerResponse
        {
            Uid = table.Uid,
            Nickname = command.Nickname,
        };
    }
}
