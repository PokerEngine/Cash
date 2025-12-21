using Application.Repository;
using Application.Service.Hand;
using Domain.Entity;
using Domain.Event;
using Domain.ValueObject;

namespace Application.Command;

public record struct SitDownPlayerCommand : ICommandRequest
{
    public required Guid TableUid { get; init; }
    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required int Stack { get; init; }
}

public record struct SitDownPlayerResponse : ICommandResponse
{
    public required Guid TableUid { get; init; }
    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required int Stack { get; init; }
}

public class SitDownPlayerHandler(
    IRepository repository,
    IHandService handService
) : ICommandHandler<SitDownPlayerCommand, SitDownPlayerResponse>
{
    public async Task<SitDownPlayerResponse> HandleAsync(SitDownPlayerCommand command)
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

        return new SitDownPlayerResponse
        {
            TableUid = table.Uid,
            Nickname = command.Nickname,
            Seat = command.Seat,
            Stack = command.Stack
        };
    }
}
