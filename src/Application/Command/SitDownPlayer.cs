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

            var handUid = await handService.CreateAsync(
                tableUid: table.Uid,
                game: table.Game,
                maxSeat: table.MaxSeat,
                smallBlind: table.SmallBlind,
                bigBlind: table.BigBlind,
                smallBlindSeat: table.SmallBlindSeat,
                bigBlindSeat: (Seat)table.BigBlindSeat!,
                buttonSeat: (Seat)table.ButtonSeat!,
                participants: table.ActivePlayers.Select(GetParticipant).ToList()
            );

            table.StartHand(handUid);
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

    private HandParticipant GetParticipant(Player player)
    {
        return new HandParticipant
        {
            Nickname = player.Nickname,
            Seat = player.Seat,
            Stack = player.Stack
        };
    }
}
