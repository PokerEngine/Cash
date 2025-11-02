using Application.Repository;
using Domain.Entity;
using Domain.Event;

namespace Application.Command;

public record PlayerStandUpCommand(
    Guid TableUid,
    string Nickname
) : ICommand;

public record PlayerStandUpResult(
    Guid TableUid,
    string Nickname
) : IResult;

public class PlayerStandUpHandler(
    IRepository repository
) : ICommandHandler<PlayerStandUpCommand, PlayerStandUpResult>
{
    public async Task<PlayerStandUpResult> HandleAsync(PlayerStandUpCommand command)
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

        return new PlayerStandUpResult(
            TableUid: table.Uid,
            Nickname: command.Nickname
        );
    }
}
