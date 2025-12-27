using Application.Event;
using Application.Repository;
using Application.Service.Hand;
using Domain.Entity;
using Domain.ValueObject;

namespace Application.Command;

public record struct SitDownPlayerCommand : ICommand
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
    IEventDispatcher eventDispatcher,
    IHandService handService
) : ICommandHandler<SitDownPlayerCommand, SitDownPlayerResponse>
{
    public async Task<SitDownPlayerResponse> HandleAsync(SitDownPlayerCommand command)
    {
        var table = Table.FromEvents(
            uid: command.TableUid,
            events: await repository.GetEventsAsync(command.TableUid)
        );

        table.SitDown(command.Nickname, command.Seat, command.Stack);

        if (table.HasEnoughPlayersForHand() && !table.IsHandInProgress())
        {
            table.RotateButton();

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

            table.StartHand(handState.HandUid);
        }

        var events = table.PullEvents();
        await repository.AddEventsAsync(table.Uid, events);

        foreach (var @event in events)
        {
            await eventDispatcher.DispatchAsync(@event, table.Uid);
        }

        return new SitDownPlayerResponse
        {
            TableUid = table.Uid,
            Nickname = command.Nickname,
            Seat = command.Seat,
            Stack = command.Stack
        };
    }
}
