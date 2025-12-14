using Application.Repository;
using Domain.Entity;
using Domain.Event;

namespace Application.Command;

public record StandUpPlayerCommand(
    Guid TableUid,
    string Nickname
) : ICommandRequest;

public record StandUpPlayerResult(
    Guid TableUid,
    string Nickname
) : ICommandResponse;

public class StandUpPlayerHandler(
    IRepository repository
) : ICommandHandler<StandUpPlayerCommand, StandUpPlayerResult>
{
    public async Task<StandUpPlayerResult> HandleAsync(StandUpPlayerCommand command)
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

        return new StandUpPlayerResult(
            TableUid: table.Uid,
            Nickname: command.Nickname
        );
    }
}
