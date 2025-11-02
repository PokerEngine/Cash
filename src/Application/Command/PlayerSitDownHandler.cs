using Application.Repository;
using Domain.Entity;
using Domain.Event;

namespace Application.Command;

public record PlayerSitDownCommand(
    Guid TableUid,
    string Nickname,
    int Seat,
    int Stack
) : ICommand;

public record PlayerSitDownResult(
    Guid TableUid,
    string Nickname,
    int Seat,
    int Stack
) : IResult;

public class PlayerSitDownHandler(
    IRepository repository
) : ICommandHandler<PlayerSitDownCommand, PlayerSitDownResult>
{
    public async Task<PlayerSitDownResult> HandleAsync(PlayerSitDownCommand command)
    {
        var table = Table.FromEvents(
            events: await repository.GetEventsAsync(command.TableUid)
        );

        var eventBus = new EventBus();
        var events = new List<BaseEvent>();
        var listener = (BaseEvent @event) => events.Add(@event);
        eventBus.Subscribe(listener);

        table.SitDown(
            nickname: command.Nickname,
            seat: command.Seat,
            stack: command.Stack,
            eventBus: eventBus
        );

        eventBus.Unsubscribe(listener);

        await repository.AddEventsAsync(table.Uid, events);

        return new PlayerSitDownResult(
            TableUid: table.Uid,
            Nickname: command.Nickname,
            Seat: command.Seat,
            Stack: command.Stack
        );
    }
}
