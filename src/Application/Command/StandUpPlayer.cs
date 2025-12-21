using Application.Repository;
using Domain.Entity;
using Domain.Event;

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
            events: await repository.GetEventsAsync(command.TableUid)
        );

        var eventBus = new EventBus();
        var events = new List<BaseEvent>();
        var listener = (BaseEvent @event) => events.Add(@event);
        eventBus.Subscribe(listener);

        table.StandUp(
            nickname: command.Nickname,
            eventBus: eventBus
        );

        eventBus.Unsubscribe(listener);

        await repository.AddEventsAsync(table.Uid, events);

        return new StandUpPlayerResponse
        {
            TableUid = table.Uid,
            Nickname = command.Nickname
        };
    }
}
