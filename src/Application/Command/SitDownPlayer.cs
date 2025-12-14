using Application.Repository;
using Application.Service.Hand;
using Domain.Entity;
using Domain.Event;
using Domain.ValueObject;

namespace Application.Command;

public record SitDownPlayerCommand(
    Guid TableUid,
    string Nickname,
    int Seat,
    int Stack
) : ICommandRequest;

public record SitDownPlayerResult(
    Guid TableUid,
    string Nickname,
    int Seat,
    int Stack
) : ICommandResponse;

public class SitDownPlayerHandler(
    IRepository repository,
    IHandService handService
) : ICommandHandler<SitDownPlayerCommand, SitDownPlayerResult>
{
    public async Task<SitDownPlayerResult> HandleAsync(SitDownPlayerCommand command)
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

        if (table.HasEnoughPlayersForHand() && !table.IsHandInProgress())
        {
            table.RotateButton(
                eventBus: eventBus
            );

            var handState = await handService.CreateAsync(
                tableUid: table.Uid,
                game: table.Game,
                maxSeat: table.MaxSeat,
                smallBlind: table.SmallBlind,
                bigBlind: table.BigBlind,
                smallBlindSeat: table.SmallBlindSeat,
                bigBlindSeat: (Seat)table.BigBlindSeat!,
                buttonSeat: (Seat)table.ButtonSeat!,
                participants: table.GetParticipants()
            );

            table.StartHand(
                handUid: handState.HandUid,
                eventBus: eventBus
            );
        }

        eventBus.Unsubscribe(listener);

        await repository.AddEventsAsync(table.Uid, events);

        return new SitDownPlayerResult(
            TableUid: table.Uid,
            Nickname: command.Nickname,
            Seat: command.Seat,
            Stack: command.Stack
        );
    }
}
