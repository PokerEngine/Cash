using Application.Repository;
using Domain.Entity;
using Domain.Event;

namespace Application.Command;

public record StandUpFromTableCommand(
    Guid TableUid,
    string Nickname
);

public record StandUpFromTableResult(
    Guid TableUid,
    string Nickname
);

public class StandUpFromTableHandler(
    IRepository repository
) : ICommandHandler<StandUpFromTableCommand, StandUpFromTableResult>
{
    public async Task<StandUpFromTableResult> HandleAsync(StandUpFromTableCommand command)
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

        return new StandUpFromTableResult(
            TableUid: table.Uid,
            Nickname: command.Nickname
        );
    }
}
